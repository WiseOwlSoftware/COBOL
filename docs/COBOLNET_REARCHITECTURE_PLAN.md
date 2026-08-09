# COBOL.NET — THE PLAN (the ONE rearchitecture + 100%-ISO roadmap · live resume state · phase execution detail)

> **⛔ THIS IS THE ONLY PLANNING DOCUMENT (owner directive 2026-07-19).** It replaced and absorbed
> `resume-prompt.md`, the 17 per-phase step-by-step docs (`PHASE-00..16-*.md`), `PHASE-13-audit.md` (+ its scout
> JSON), and `PLAN-bindtime-gating-migration.md` — all deleted; their live content is IN this document (Part II
> = live-phase execution detail; Part III = completed-phase records; §8 = the consolidated residue ledger).
> **Every session: read §0 FIRST; update §0 + the affected worklists BEFORE ending.** History lives ONLY in
> `DEVLOG.md`. Design SSOTs stay separate (`docs/COBOLNET_DESIGN.md` + the `DESIGN-*.md` deep-dives) — this doc
> plans work; those specify design. Evidence ledgers stay separate (the P13 review ledger, `PHASE-*-scout-notes`).

## §0 — LIVE RESUME STATE (the ONLY live-state SSOT; keep it current every session)

**THE SINGLE-WRITE RULE:** live state is written ONLY here — every other section POINTS here, never duplicates.
Two registers own their own tally and are likewise never restated here: the fix-queue's LANDED header and
`DEVLOG.md`. If you find live state written twice, fixing that is part of the session. **§0 states WHERE WE ARE,
never how we got here — narrative belongs in `DEVLOG.md`.**

**SESSION BOOTSTRAP (a new session does exactly this):** ① read `CLAUDE.md` (the non-negotiable rules) → ② this §0
→ ③ `git checkout phase-14` and run **`pwsh scripts/session-probe.ps1`** (the mechanical state check: branch ·
dirty/unpushed · next-free diagnostic · VCR todos · corpus counts · inventory GAP) → ④ work the top of **NEXT**
below — spec-first, § cited, complete-not-test-scoped; design questions → the `docs/COBOLNET_DESIGN.md` §0.5
deep-dives; fixes → the queue entry, which carries the exact verified fix → ⑤ before ending: update THIS §0 and add
a DEVLOG entry per commit; commit AND push every checkpoint.

### Where we are

- **▶ SESSION HANDOFF (2026-08-09 13:30 — the owner's STANDING DIRECTIVES, all still in force):
  ① FIX THE KNOWN DEFECT BACKLOG TO ZERO — no new adjudication exploration until it reads zero
  (owner, 2026-08-09). The backlog = inventory rows with DIVERGES/PARTIAL/NOT-IMPLEMENTED verdicts;
  session-probe/`work.py next` rank the fix queue. ② Run fully autonomously, no permission pauses;
  only D13-class spec decisions are owner-reserved. ③ ALL subagents run Opus 5
  (`CLAUDE_CODE_SUBAGENT_MODEL` is set persistently; also pass `model:'opus'` in workflow scripts).**
  **WHERE THE CAMPAIGN STANDS (all numbers COMPUTED — run the probe):** the 2026-08-09 triage
  (DEVLOG 1255, evidence frozen in `docs/rearchitecture/backlog-triage-2026-08-09.json`) probed all
  331 backlog rows with a 14-group probe+refute fleet: 114 verified fixed by the earlier waves, the
  live remainder clustered into register notes **PB58–PB66** (+PB56 landed, PB67 = owner decision).
  Six fix landings followed the same day (DEVLOG 1257–1262): PB56's Dec-carrier bodies, the 39-digit
  alignment wrap, date-windowing arity overloads, domain guards, wide-value members, and the MOVE
  Dec-store channel — backlog 331 → 185, each closed row carrying probe evidence + a spec-derived
  test-ref in its inventory note.
  **THE FIX LOOP (proven 6×, follow it):** read the cluster note → derive from the spec (`cite.py
  --check` every citation) → fix at the root, sweep siblings → CLI-probe BEFORE writing the .out →
  golden + manifest same commit → wave-local gate (full suites when a shared seam moves) →
  `record_verdicts.py` re-verdict batch → SpecTraceability gate → note/DEVLOG/§0 → commit AND push.
  **NEXT, in queue order:** PB65's last three singles (BOOLEAN-OF-INTEGER's 63-bit AsInt bridge ·
  Power's receiver-arm swap RV-15.64.4-1 · the native FromDouble clamp-sentinel family
  RV-15.75.4-1) → **PB59** (CONVERT/Repertoire, 28 rows, silent wrong answers) → **PB60** (NUMVAL
  real parsers, 13) → **PB58** (argument-screen table shape, 41) → PB61 → PB62 → PB63 → PB64 (LOCALE
  feature — deep-dive design doc FIRST, rule 2) → PB66 (external float PICTURE — design first).
  **OWNER-RESERVED, ask one at a time when he is at the keyboard:** PB67 (the four standard-binary
  rows → DOCUMENTED-NON-SUPPORT?) · CURRENT-DATE's +14:00 spec hole (RV-15.21.3-1) · PB55's
  IMPLEMENTATION is decided (rule-is-the-unit, 2026-08-08) and still needs its transcription
  re-indent + 234-row absorption campaign.
  **HAZARDS THIS CAMPAIGN KEEPS HITTING:** the one-of-two-callers/two-arm shape (found again 4× —
  always ask which arm); pre-PB56 triage rows may already be fixed (probe before fixing); MOVE
  renders sources under ReceiverContext.None by design (its 39-digit receiverless-selection escape
  raise is DOCUMENTED residue on PB65, not a bug to "fix" casually).
  (The 2026-08-08 catalog-campaign handoff is DISCHARGED: PB39 DEVLOG 1252, PB29 DEVLOG 1253,
  denominator 4,311, the content-keyed scan at zero pending, blind-spot class closed and guarded.) Everything else is CLOSED:
  **the R-wave (R21–R39) is fully landed** — `work.py next` reports the ranked actionable list EMPTY —
  including R13, decided by the owner 2026-08-08 (**follow GnuCOBOL** on the beyond-PICTURE COMP-5
  DISPLAY; §7 item 56; and the broader preference — implementor latitude + split vendors → follow
  GnuCOBOL — is in session memory as `follow-gnucobol-on-split-latitude`, LATITUDE ONLY — and the vendor axis is
  now CLOSED by owner decision the same evening: COBOL.NET stays STRICT-ISO permanently, GnuCOBOL's
  non-ISO extensions are never implemented; R36/R37/R38/R39 carry the record). The battery reference below is current at the R33+R38 closing tree; CI green
  through `b66a292c`. After PB29+PB39, what remains is the A-series analyses (§11) and the P14
  campaigns proper.
- **▶ THE PRIOR CAMPAIGN (2026-08-08): the R00-audit orphan wave — ELEVEN items landed in one session,
  NINE register notes remained open at its handoff, and `kb/Work/` owned every one of them** (`python scripts/spec/work.py
  next` — 6 actionable at session end, headed R21 · R22 · R25; the rest are adjudications/decisions).
  The wave's provenance: R00's full-ledger complement measurement dispositioned all 82 batch-4 findings BY
  PROBE (62 measured fixed, 20 open → notes R14–R28, forensics + repro shapes in each note body — DEVLOG
  1218). Landed since, in order: **R10** (unsigned COMP-5 carriers `ulong`/`UInt128` — the container-range
  ownership decision, CONFORMANCE.md item 208) · **R14** (the ambient exception-status (statement, location)
  channel + the same-line GR14 `Inclusive` fix) · **R18** (EXP/EXP10/E/PI under the standard modes via the
  ONE `CobolDec.Pow`) · **R24** (the FORMATTED-* seconds channel carrier-total; `AsInt`'s missing Dec arm) ·
  **R15** (keyword-omitted INSPECT) · **R16** (`COBOLNET1637` — §13.18.38.3 r7's closed index-name context
  list; spawned R29) · **R12** (the CALL ABI crosses every native carrier TYPED — `CobolArgAdapt.Num<T>`,
  no image; the probe showed the old wide-tier crossing was UNCOMPILABLE, not silently lossy) · **R17** (the
  signed float literal's lexer twins + `SignedLiteralShapeDriftTests`) · **R19** (`COBOLNET1638` for phrase
  words as arguments) · **R20** (FIND-STRING's positional phrase walk). Each note's LANDED section carries
  the implementation record; DEVLOG 1217–1228 the narrative. Next-free diagnostic: session-probe computes it
  (1639 unless something landed since).
  ✅ **THE R17+R19+R20 COMPREHENSIVE BATTERY COMPLETED `ALL GREEN` (2026-08-08 13:38)** — every leg,
  including the differential at **0 per-case flips** on the R17 grammar change, the leg the handoff said
  to watch. The battery reference (§9 Gates) carries the numbers (single-write rule); the wave continues
  from the top of `work.py next`.
  **The session then landed R21 (the DateTimeOffset clock seam) · R22 (COBOLNET1543 for every catalogued
  name) · R30 (COBOLNET1639 — an undeclared name reports at compile time; the Resolve/Probe split) ·
  R31 (qualified matching goes candidate-set) · R32 (screen names registered), and the R21+R22+R30
  comprehensive battery's differential leg — 41 flips, all attributed, see the battery reference —
  spawned register notes R33–R38.** R22's push briefly broke CI on a GREEN TEST PINNING ITS OWN DEFECT
  (KeywordOmitted_RequiresRepositoryDeclaration_RuntimeLoudFail asserted the run-time loud-fail;
  rewritten to the compile-time COBOLNET1543 with the R30 landing) — the wave-local filter never matched
  that class name, which is why the full legs ran before every later landing.
- **⛔ THE REPO LAYOUT CHANGED 2026-07-27.** `specs/` is now an ORDINARY PUBLIC DIRECTORY holding
  `specs/ISO_COBOL.md`; the private submodule moved to **`specs-private/`** and holds only the licensed PDF.
  The Markdown path is unchanged, so all 1,663 spec citations still resolve. Tools that MEASURE the printed page
  resolve `specs-private/` and say so if it is absent: `git submodule update --init specs-private`.
- **Page citations in the transcription are PRINTED FOLIOS**, not PDF pages. The `#page-N` anchors remain PDF
  sequence, which runs **folio + 30** (30 pages of front matter). Clause references (14.9.41.2) are unambiguous
  either way and are the better citation.
- **✅ MERGED TO `main` (2026-07-30, `1d0c24c9`) — 33 commits: the V59 wave, DA1–DA7, and the P14 Step-0
  MECHANISM wave through PB6.** ⚠ **WORK HAS CONTINUED ON `main` SINCE** (PB7, the PB1 table extension, Phase-B
  batches 2 and 3), so the merge commit is a milestone in the history, NOT the head. `phase-14` and `main` were
  at the same commit at the merge and are kept in step; **verify, do not assume** — the probe computes it.
  ⛔ **THAT SENTENCE GOES STALE THE NEXT TIME ANYONE COMMITS, and the previous version of this line is why the
  warning is here**: it said the trees were identical and stayed said while a whole wave landed on top.
  **Never read a commit count or a branch relationship from this document** — run
  `git rev-list --count main..phase-14`, or `pwsh scripts/session-probe.ps1`, which computes it.
  **THE PRE-MERGE GATE, every leg measured at the merge (not carried):** Conformance **4148/4148** zero skipped ·
  Unit **963/963** · characterization **33/33** · `guard-fast.sh` **ALL GREEN** with NIST **353 MATCH /
  0 REGRESSION**, legacy Unit 1203/1203, Integration 503/504 (1 skipped) · GnuCOBOL differential **10 per-case
  flips, ALL ATTRIBUTED, 0 unexplained**. Conformance and characterization were re-run ON THE MERGE COMMIT
  ITSELF, because a merge produces a tree neither branch was tested as.
  ⚠ **The differential found PB6, which no other leg could see** — and six of its eight FIXES are PB2, a
  runtime-shaped defect that manifested as a FAILED COMPILE. §0 records this net as "structurally blind to a
  change in RUNTIME OUTPUT", which is true and incomplete: a runtime defect that stops the program COMPILING is
  fully visible to it.
  ⚠ **DEVLOG 1112 and this section were previously stamped `21:40`/`21:45 PDT` on 2026-07-29, but `date` returned
  `19:05 PDT` while 1113 was being written** — the earlier stamps are ~2.5 h in the future, so entry 1113 correctly
  reads as EARLIER than 1112 above it. Stamp from `date`; do not manufacture a later time to preserve the look.
- **THE STATE OF THE CONFORMANCE REVIEW — the numbers that matter, all computed, none carried.**
  `pwsh scripts/session-probe.ps1` prints them; the shape is what this bullet is for. **The denominator is 3,861
  normative rules** (corrected from a short 3,790 — see the caution below). Roughly 180 rules are adjudicated,
  all in §15 (the intrinsic functions), leaving the GAP a little under 3,800. **Never quote a number from this
  paragraph — run the probe.** FOUR Phase-B batches have run, each fanned out one agent per function and then
  handed to an independent agent told to OVERTURN; every batch's overturns were downgrades.
- **THE FIX QUEUE IS LIVE AGAIN AND IS FED BY THE REVIEW.** ⛔ **WHAT IS OPEN AND WHAT IS LANDED IS NOT WRITTEN
  HERE — `kb/Work/` OWNS IT** (CLAUDE.md rule 8; `python scripts/spec/work.py next`, and session-probe prints it
  every session). The enumeration that used to sit on this line was a worklist, which §0 may not carry, and it
  rotted exactly the way rule 8 predicts — it listed landed items as live. **No BLOCKER is open.** What belongs
  here is the SHAPE of what the review found, not its inventory: three of the landed were blockers and every one
  was SILENT — the pattern this review exists to catch:
  · **PB5** — the float→fixed quantizer saturated at |value| ≈ 9.2 × 10⁹, so `FUNCTION ANNUITY(1e10 1)` into an
    ordinary `PIC 9(12)V99` money field returned 9223372036.85 for 10000000001.00, with **NO SIZE ERROR**.
  · **PB7** — every ZERO-ARGUMENT intrinsic was unreachable in the keyword-omitted form:
    `REPOSITORY. FUNCTION ALL INTRINSIC.` + `MOVE CURRENT-DATE TO X` compiled clean and threw at RUN TIME.
  · **PB13** — the SAME quantizer as PB5, one layer up: the working scale was chosen without reference to the
    receiver's capacity, so the saturation sentinel was rescaled back INTO range and the digit-capacity check
    never saw it. `COMPUTE R = FUNCTION EXP(70)` into `PIC 9(31)` was wrong by ~15× with no SIZE ERROR, and
    `FUNCTION EXP10(30) = FUNCTION EXP10(31)` was TRUE. ⛔ **Its sweep found PB5's own defect still live in the
    NUMVAL family** — `FUNCTION NUMVAL-F("1E+20")` returned 9223372036 — which is why "fix the root cause, then
    sweep for siblings" is a rule and not advice: PB5 fixed one call site of a clamp that had four.
  The rest: PB1 (the §15.3 argument-class screen, `COBOLNET1627`) · PB2 (a float argument routed to a `…Real`
  body instead of emitting a raw `CS1503`) · PB3 (ORD past the 256-entry collating table) · PB4 (a hexadecimal
  literal decoded in VALUE / ALL / 88 / OCCURS) · PB6 (`CALL BY VALUE` screened by §14.9.4.3 SR22,
  `COBOLNET1628`) · **PB8** (reference-modifying a FUNCTION result — §8.4.3.3.3 SR2/SR3 + §8.4.3.2.3 SR6,
  `COBOLNET1629`/`COBOLNET1630`). Each landed with a spec-derived golden.
- **⛔ A QUEUE ENTRY'S OWN "ROOT CAUSE, ALREADY LOCATED" IS A CLAIM, NOT A FACT — PB8 IS THE STANDING PROOF.**
  Its entry named a LEXER-MODE defect and called the fix "the riskiest category in this codebase"; a token dump
  showed both failing shapes were already lexed in DEFAULT mode and **the lexer was never touched.** The entry
  had been REASONED from the lexer's source, never MEASURED. Re-measure a named root cause before budgeting for
  it (`use_antlr_tree_dump`); `CobolLexerModeDriftTests` now pins the mode per shape so it cannot rot.
- **⚠ TWO LANDED FIXES SHIPPED A DEFECT A GREEN BATTERY CANNOT SEE — the pattern, not the incident.** `COBOLNET1627` (PB1) and
  `COBOLNET1628` (PB6) emitted their kebab `Id` instead of their `Code`, so the code `DIAGNOSTICS.md` documents
  and `--suppress` matches was never printed. Root cause was an API asymmetry — `Error` had a descriptor
  overload, `Warning` did not — so a `--permissive` site had nothing correct to reach for. Both overloads now
  exist and `DiagnosticEmitFormDriftTests` is a SOURCE-FORM guard, because no runtime test can see a mistake
  only a caller can make.
- **⛔ THE LESSON THIS REVIEW KEEPS RE-TEACHING, in three forms.** Carry it into the next batch:
  · **One rule written down more than once** (`feedback_one_rule_one_place`) — PB4's prefix list was duplicated
    inside ONE file and both copies omitted `X`; PB3's tail arithmetic existed correctly on the national side and
    not the alphanumeric one.
  · **A table nothing reads has never been contradicted** (`feedback_a_dead_lookup_is_also_unverified`) — PB1's
    `ArgKinds` column was not merely unread but UNVERIFIED, and enforcing it as written rejected 12 legal corpus
    programs. Re-derive a dead table before wiring it in.
  · **A code that stands for two rules enforces neither** — screening `'n'` (class numeric) and `'i'`
    (§15.3 type 6 integer) identically was recorded here as wrongly rejecting `FUNCTION CHAR(<numeric-edited>)`,
    "because type 6 admits an arithmetic expression and a numeric-edited item de-edits".
    ⛔ **THAT SECOND CLAUSE WAS FALSE, AND IT IS NOW AN OWNER DECISION (2026-08-02): a numeric-edited item is
    NOT an arithmetic operand and NOT an integer argument.** §8.8.1.1 admits "an identifier referencing a
    NUMERIC data item"; §8.5.2.13 calls this a "numeric-edited data item" — a distinct defined term — and
    §8.5.2.1 Table 2 puts that category in class ALPHANUMERIC or NATIONAL, never numeric; §15.3 type 6's only
    other alternative is "an integer data item"; and de-editing is GRANTED by the MOVE rules (§14.9.25.4 GR6d1)
    and nowhere extended to arithmetic. The `'n'` arm had already refuted the identical reading, so the two arms
    rested on readings of §8.8.1.1 that could not both be right. Both external oracles agree: no NIST program
    depends on it, and GnuCOBOL exercises de-editing only under MOVE. **The surviving lesson is narrower and
    sharper — a golden and a unit test agreeing is NOT independent evidence when both were written from the
    same premise.** DA6's screen, `IntrinsicArgumentRules`'s `'i'` arm, one corpus golden and one AssertSpec
    test all moved together; the rejection is pinned by `pb1-integer-arg-numeric-edited`.
- **⚠ THE INSTRUMENTS HAVE LIED MORE OFTEN THAN THE COMPILER HAS.** Every one of these cost real time:
  · a citation audit reported **133 defects of which essentially none was real** (it matched quoted LABELS and
    doc-internal `§` refs); the precise form — text that IS in the spec, filed under the WRONG clause — found 9.
  · `guard-fast` reported **exit 1 on a green run** because the command chain ended in a `grep -c` that found
    nothing. **Gate on the verdict line, never the exit code** (`feedback_gate_on_the_verdict_line`).
  · a **workflow reported "completed" with 5 of 12 agents dead on API 529**, leaving unrefuted stage-one output
    in their files. Read the notification's `<failures>` block before its results.
  · reading a two-stage workflow's output files EARLY published a wrong GAP number. **Every overturn is a
    downgrade, so an early read biases the result UPWARD.** Wait for the completion notification.
- **⛔ THE DENOMINATOR WAS SHORT TWICE AND IS NOW 3,861.** First by 56 (3,790 → 3,846): ten rules sat under
  headings the extractor's literal-spelling map did not know (`Argument`**`s`** ` rule(s)`,
  `Returned `**`values`**` rules`), the rest accumulated while the catalog was frozen behind a halt.
  Then by 15 (3,846 → 3,861) — see the decision below. Pluralisation is NORMALISED and a guard reports any
  rule-shaped heading the map cannot resolve. **Every percentage quoted against 3,790 or 3,846 flattered.**
  ⚖ **OWNER DECISION 2026-07-30 — TAKEN, and the premise it was posed under did not survive contact with §5.3.**
  It was framed as "§13.18.40.5 Editing rules, §13.18.40.6 Precedence rules and §5.3 Rules are rule KINDS, so
  admitting them changes what the denominator MEANS". **The standard classifies them itself** (both validated
  with `cite.py --check`): §5.3.3 — "The rules of the PICTURE clause specified in 13.18.40.5, Editing rules, are
  **General rules**"; §5.3.2 — "…13.18.40.6, Precedence rules, are **syntax rules**". So they are GR and SR under
  a heading spelling the map did not know — **the SAME defect class as the pluralisation fix, not a change of
  meaning.** Decision: admit both, TYPED BY §5.3; exclude §5.3 itself, which defines the taxonomy and carries no
  numbered rules (and is the very clause that types the other two).
  ⚠ **THE EFFECT IS +15, NOT THE +17 FIRST REPORTED.** §13.18.40.5 yields 15 GRs; **§13.18.40.6 yields ZERO** —
  its content is UNNUMBERED prose plus Tables 10 and 11, and the normative content IS those two precedence
  matrices, which have no per-rule ordinal to extract. The "2" came from a line-range scan that ran past the
  clause boundary. Both dispositions are now DECLARED IN THE EXTRACTOR (`EXCLUDED_BLOCKS` / `KNOWN_EMPTY_BLOCKS`,
  each with its reason) rather than left to reappear as a warning on every run — a guard that reports an expected
  condition forever is one people learn to ignore. The only PARSE GAP still reported is **§15.4 "Returned
  values"**, which is PRE-EXISTING (verified by running the extractor on the pre-change tree) and unowned.
- **PRECEDING WAVES, for provenance only** — V59 (one byte representation at every byte boundary; the 46-finding
  audit CLOSED, DEVLOG 1095–1102) and DA1–DA7 (the discovered-during-implementation set, DEVLOG 1103–1112).
  Narrative lives in `DEVLOG.md`; §0 states where we are, never how we got here.
- **⚠ A MEASURED LIMIT ON THE GnuCOBOL DIFFERENTIAL, corrected once already.** It compares COMPILE-TIME
  accept/reject verdicts, so it is blind to a change in RUNTIME OUTPUT — but NOT to a runtime-shaped defect that
  stops the program COMPILING. Six of eight fixes in the pre-merge run were PB2, whose symptom was a Roslyn
  `CS1503`. The accurate rule: **changes that leave compilability unaltered are invisible to it.**

### ✅ GITHUB ACTIONS IS BACK — CI is `active` again (2026-08-07)

`Build and Test` was `disabled_manually` through GitHub's Actions outage (Critical incident opened 15:22 UTC
2026-08-06: webhook triggers throttled to ~15%, ~65% of queued jobs succeeding, runners stuck retrying jobs that
no longer exist) — five consecutive pushes produced ZERO runs and one dispatched run wedged so that `cancel`
reported it completed while `force-cancel` reported a re-run that never queued. **The incident is resolved, the
wedged run settled to `completed/cancelled` on its own, and the workflow is re-enabled.**

**RE-ENTRY WAS SEQUENCED, and the sequence is the reusable part.** ① `gh workflow enable "Build and Test"` →
② dispatch ONE run (`workflow_dispatch`, not a push — `legacy-oracle` is `if: schedule || workflow_dispatch`, so
only a dispatch exercises all FIVE jobs) → ③ confirm every job gets a RUNNER, not merely that the run exists →
④ only then land the actions-version bump, so a red is attributable to the bump rather than to outage residue.
Baseline run **31141695885 — all five jobs SUCCESS** on `ab691eff` with the OLD action versions; the bump
(`checkout` v4→v7, `cache` v4→v6, `setup-dotnet` v4→v6 — 15 references, re-verified against
`repos/{a}/releases/latest`: v7.0.1 / v6.1.0 / v6.0.0) landed on top of that baseline and is **GREEN**:
push run **31143400444, all four push jobs SUCCESS** — `legacy-oracle` reports `skipped`, which is the
`if:` guard doing its job on a push, not a failure. That run also proves the **push trigger** recovered, which
a dispatch cannot show.

⚠ **A BURST OF COMMITS LEAVES ONLY THE LAST ONE WITH A CI VERDICT, and the runs it cancels say `cancelled`, not
`failure`.** The workflow sets `concurrency: { group: workflow-ref, cancel-in-progress: true }`, so each push to
`main` kills the run in flight. A full run is ~25 min; four commits landed inside that window on 2026-08-07 and
**three of the four runs read `cancelled`** (the fast legs — Guard, INV-1-strong — had already reported success;
Greenfield and Windows never finished). ⛔ **Do not read those as reds, and do not read the surviving run as
per-commit coverage:** it validates the CUMULATIVE tree, so a red would need a bisect to attribute. The setting
is correct for a busy branch — the alternative is a queue of stale runs — so the discipline is on the reading,
not the config. When per-commit attribution actually matters (a bump, a shared-grammar change), let the run
finish before pushing again, exactly as the actions-version bump did.

⛔ **THE STANDING FACT THE OUTAGE MADE VISIBLE: `bash scripts/battery.sh` IS NOT A SUPERSET OF CI.** The battery
covers Conformance, Unit, characterization, `guard-fast` (NIST + legacy unit + integration) and the GnuCOBOL
differential — but it runs on **WINDOWS in DEBUG**. Three of the five CI jobs are `ubuntu-latest` and the guard
leg is Linux-only bash, so **the Linux legs and the Release configuration are verified by CI and by nothing
else**. That was true before the outage too; the outage is merely what made a week of it unverifiable. Keep the
battery as the per-batch local gate and let CI own the cross-platform axis.

### ⛔ SESSION HANDOFF — READ THIS BEFORE THE TABLE BELOW.

**⚙ 2026-08-07 EVENING SESSION CLOSE — R04 · R05 · R06 · R07 · R08 · R09 (closed stale) · R10 (float half) —
and the two NEXT items are fully planned in their `kb/Work/` notes.**
⛔ Run the probe; the worklist is `kb/Work/` (`python scripts/spec/work.py next`), never this paragraph.

**START HERE NEXT SESSION — two items, each carrying its complete plan in its note:**
1. **R10's COMP-5 half — OWNER DECISION TAKEN (2026-08-07): UNSIGNED CARRIERS.** Unsigned COMP-5 emits `ulong`
   (10–18 digits) / `UInt128` (19–31). One change set: `PicInfo.ClrType` / `DataItem.ElementType` storage tier ·
   `CobolNum.WrapBinary`/`InBinaryRange` rework (fixing the bits=128 shift-mask bug where the modulus collapses
   to 1 — F73) · the arithmetic render paths · the drift test pinning `BindAlgebraicFold`'s container-usage
   list against `PicInfo`'s `NumericTruncation.BinaryCapacity` list (one list written twice). Plan: `kb/Work/R10.md`.
2. **R00 — the ledger audit.** The old residue block enumerated exactly 10 findings (became R01–R10) while
   claiming 16; the evidence ledger holds 82. Treat the COUNT as a hypothesis: disposition every F-finding
   (R-note · PB cluster · landed-and-PROBED — R09 was closed stale, a note's word is not evidence · ORPHAN);
   each orphan becomes an R11+ note first. Recipe + the cross-references already established: `kb/Work/R00.md`.
   Prime orphan candidates: F7–F11 (function-identifier operand family, DISPLAY-of-index), F12 (signed float
   literal arguments).

⭐ **THE SESSION'S FINDING — the premise-failure pattern now stands at 14 of 16:** R04 (the token axis cannot
name GO TO — `TO` is optional), R05 (the "truncation" was §15.33.3 r1 itself; the real defects were the missing
record, the advisory, and a directive-word §8.3.2.1 hole the item's own fact discovered), R09 (already fixed by
PB25 — closed on a PROBE, not the comment). R06 and R08 are the only two whose premises held as filed.
Three structural extractions landed en route, each with a drift guard: `Table12StatementNames` (statement-kind →
Table 12 name), `EcNameResolution` (the ONE written-exception-name funnel — it also closed the USE-site level-2
edition-gate hole), and `CobolWordRule` (the ONE §8.3.2.1 length ceiling — the tree walk AND the directive
stages, closing the `>>TURN`/`>>DEFINE` evasion). R07's lesson: `ExceptionState` is an ENGINE + a STATIC
FACADE — editing one arm fails CS1501 at generated-code compile time; check both.

**⚙ 2026-08-07 (morning) SESSION CLOSE — PB46 (both halves) · PB27 · PB35 · PB50 · PB54 · PB52 · PB53 · R03 · R01 · R02.**
⛔ Run the probe; never quote a count from this paragraph — `kb/Work/` says what is OPEN
(`python scripts/spec/work.py next`), and it is down to residue items.

⭐ **THE PATTERN, AND IT HELD ON EVERY SINGLE ITEM: THE ENTRY'S STATED DEFECT WAS NOT THE DEFECT.** Wrong layer,
wrong count, wrong clause, or a blocker that did not exist:
· **PB46** — "thread a boolean channel through four layers"; underneath, a `default:` arm made FIVE PICTURE
  categories impossible as method parameters. Its CALL half was filed "blocked on the P13 prototype registry"
  and was not blocked at all: §14.9.4.2 Format 2 is selected by a SYNTACTIC `AS` phrase whose NESTED arm needs
  no registry. **Three of this one item's premises failed against its own general format.**
· **PB27** — "silently accepted"; it was loud, and printing a FALSE diagnostic beside the true one.
· **PB35** — named MAX and MIN; six clauses carry the rule and one was implemented.
· **PB50** — "no arm in the token switch"; PB42 had added that arm, and there were two OTHER causes, neither
  fixable alone.
· **PB52** — three causes; one had already been fixed by PB31, another was five functions rather than one.
⭐ **AND FOUR SWEEPS FOUND SOMETHING WORSE THAN THE ITEM BEING WORKED** — most sharply **PB54**, a SILENT WRONG
ANSWER where `MIN(ZERO + 5, 2)` returned 0 while `FUNCTION MIN(ZERO + 5, 2)` returned 2, for a spelling
§8.4.3.2 SR2 makes the same reference.

⛔ **THE SESSION'S OWN HARDEST LESSON: A GREEN LOCAL BATTERY CERTIFIED A TREE THAT COULD NOT BUILD FROM
SCRATCH.** A tool edit wrote `CobolLexer.g4` with a UTF-8 BOM (Python's `utf-8-sig` STRIPS on read and ADDS on
write), ANTLR rejected it, and the Windows Release job failed on `main` — while Conformance 2686/2686 and Unit
4029/4029 passed locally, because the incremental build had already produced the generated parser and never
re-ran the grammar compiler. **The owner noticed before I did.** `SourceEncodingDriftTests` is the byte-level
guard; it immediately found FOUR more BOM'd files a manual sweep had missed. See DEVLOG 1203.
⚠ Compounding it: DEVLOG 1198 — written the day before — describes the `cancel-in-progress` hazard that hid it
(a burst of commits leaves only the LAST with a verdict). **Writing a hazard down is not acting on it.** After a
push that touches the build, watch the run to completion.

**⚙ 2026-08-05 SESSION CLOSE — EVERY ITEM GATED BEFORE IT LANDED.**
⛔ **THE COMMIT/ITEM TALLY THAT USED TO OPEN THIS PARAGRAPH IS GONE, AND ITS LAST VALUE WAS WRONG.** It read
"THIRTEEN COMMITS, ELEVEN FIX-QUEUE ITEMS" against an actual ten commits — a hand-count in a paragraph whose own
next line says never to quote a number from it, and the session spans two days so no single count is even
well-defined. Counts come from `git log` and `python scripts/spec/work.py stats`; this paragraph carries the
NARRATIVE.
**PB45 · PB23 · PB25 · PB47 · PB48 all CLOSED**, and every one of their sweeps filed further defects — ⛔ **what
is OPEN is `kb/Work/`'s answer, not this paragraph's** (`python scripts/spec/work.py next`). ⭐ **THE PATTERN
ACROSS ALL FIVE: the spec-derived golden found a defect the queue
entry never mentioned** — PB45's entry did not know `EVALUATE TRUE / WHEN <level-88>` was broken, PB23's named one
intrinsic where the shared analyzer crashed THREE, PB25's blamed the function arm when the SPECIAL-NAMES
declaration was the parse error, PB47's own first implementation rejected legal source until the gate caught
it, and PB48's entry named ONE arm of four — its probe matrix then turned up a WRONG ANSWER
(`FUNCTION MAX(ZERO "A")` returning `"0"`) that no part of the report had predicted.
Writing the test from the general format rather than from the reported symptom is what found each one.
⭐ **PB48 also earns a rule about WHERE a decision belongs:** a lexical adjacency pass was answering "is this
figurative ZERO numeric or a character", which §8.3.3.6.4 GR4 makes a question about the CONTEXT — the
function's §15.3 argument type — that no token-level pass can see. The repair was to give the lexer's existing
`_fnParenStack` a TOKEN TYPE (`FNARG_LPAREN`/`FNARG_RPAREN`, §8.4.3.2.3 SR6) so the downstream pass stopped
having to guess, rather than to teach the pass a second copy of the predicate.
⚠ And its first cut encoded SR6's CONCLUSION without SR6's PRECONDITION ("if a function's definition **permits
arguments**" is a CATALOG question), which broke three PB8 ref-mod cases; `refModPart` accepts both paren
flavours for exactly that reason.
⛔ Run the probe; never quote a number from this paragraph. `kb/Work/` says what is OPEN, not this section.
LANDED: **PB17 + PB41 + PB42** (function-identifier subscripts / scaled ordinal positions / `**` and decimal
literals) · **PB43** (`USAGE BIT` occupies bits) · **PB24** (closed — its last sub-item was PB43's symptom) ·
**PB26** (the EC-ARGUMENT-FUNCTION gate) · **PB36** (INVOKE activation + r9 STACK) · **PB44** (a gate that could
false-red) · **PB45 CLOSED** (the arithmetic family + INSPECT, then the EVALUATE selection object).

**⚖ OWNER DECISION 2026-08-07 — RECONSTRUCTED, and the provenance is stated in each entry.** `dd45ddfd` (PB46's
arithmetic half), `1838a304` (PB51), `bd778375` (PB33) and `4808178a` (PB49) each landed with no DEVLOG entry;
the log ran 1191 → 1192 across them. The owner's call was **reconstruct if it can be done accurately**, and it
could: those four commit messages are unusually forensic (defect, citations, fix, what was VERIFIED, residue,
gate numbers, goldens), and each has a current `kb/Work/` note and its diff. They are now **entries 1191a–1191d**,
at their real timestamps.

⚠ **TWO CHOICES IN THAT RECONSTRUCTION ARE LOAD-BEARING.** (1) Each entry OPENS with a provenance line naming the
commit it was rebuilt from and saying it was not written at the time — the objection to reconstructing was that a
rebuilt entry is indistinguishable from a lived one, and a stated provenance is what answers it. (2) They are
**LETTERED, not renumbered**: entries 1192–1198 are already referenced by pushed commit messages ("Entry 1192",
"DEVLOG 1194"), which are immutable, so renumbering would have left the log and the git history disagreeing.
Nothing is recalled in them; every fact is carried from the sources.

**▶ WHERE THE NEXT SESSION STARTS: `python scripts/spec/work.py next`.**

⛔ **PB45's CARRY-OVER IS DISCHARGED, AND THE TOKEN DUMP REFUTED THE HYPOTHESIS THIS BLOCK USED TO CARRY.** The
previous handoff said the parenthesis discriminator "points at the SUBSCRIPT lexer mode". **It did not.** The
failing `WHEN FUNCTION SQRT(X) > 1` and the always-working `IF FUNCTION SQRT(X) > 1` lex to **byte-identical,
entirely DEFAULT-mode** token streams. The cause was `evaluateWhenGroup : NOT? evaluateWhenItem+` — a repetition
§14.9.13.2 never licensed (objects repeat ONLY through ALSO) — which let the parser PEEL the argument
parenthesis off the function and re-read it as a second selection object. ⭐ **The rule this earns: a parenthesis
is also a PARSER decision, so "the discriminator is a parenthesis" does not implicate the lexer.** That is now
three times an entry named a lexer-mode cause that a dump refuted (PB8, PB45 ×2). The warning against reordering
`evaluateWhenItem` was CORRECT, was honoured, and is now pinned by `EvaluateSelectionObjectArityDriftTests`.

⚙ **AND THE SPEC-DERIVED GOLDEN FOUND A DEFECT THE REPORT NEVER MENTIONED** — `EVALUATE TRUE / WHEN <level-88>`,
one of the commonest idioms in COBOL, threw at run time too (fixed; Table 15 + §14.9.13.4 GR4a3). It was found
ONLY because the golden was written from §14.9.13's general format rather than scoped to the reported symptom
(`feedback_spec_scopes_not_tests`), and neither external oracle could see it: the program COMPILES either way.

⭐ **THE PATTERN THIS SESSION KEPT FINDING — WORTH CARRYING IN.** THREE separate design comments justified an
omission by citing a clause that is REAL and answers a DIFFERENT QUESTION, and `cite.py --check` passes on all
three: D-B1 cited §13.18.40.4 GR14 (the representations AVAILABLE) to avoid §13.18.60.4 GR5 ("bits SHALL be
used"); PB24's entry cited §15.50.4 r9 (rounding) for a case §8.5.1.6.3 alignment actually decides;
`ModuleStack` cited §15.65.4 r3/r4 (non-COBOL elements / the FORM of a name) to justify omitting a method frame
that r5 names outright. **The check that catches this is not mechanical — ask whether the cited rule answers the
question being asked, and distrust the word "permanently".**

⚙ **AND FOUR TIMES A GREEN TEST HELD THE DEFECT OPEN** (`feedback_green_test_can_hold_a_gap_open`): the D-B1
storage-width test, `ModuleName_StackFromNestedProgram_2023`, and two of my own attempts at a drift guard that
were UNSOUND (probing an uninitialized node cannot detect a missing arm — the generated null-safe helpers make an
armless leaf and an armed-but-null leaf indistinguishable). **A green assertion resting on a misreading reads as a
decision.**

⚠ **TWO PROCESS RULES EARNED THE HARD WAY THIS SESSION, both now in the mechanics list below.**
① Do not edit compiler sources while `battery.sh` runs — phase 2 REBUILDS (broken once, and again the next day
with a "it's only a comment" excuse; both runs were stopped and re-run whole).
② Pick the wave-local filter from what the CHANGE touches, not from where the new goldens live — `~Corpus` alone
missed a MODULE-NAME test and cost a full battery run.

⚖ **OWNER DECISION 2026-08-04 — `USAGE BIT` IS IMPLEMENTED IN FULL, REVERSING D-B1's USAGE BIT HALF.**
Asked whether to implement bit-packed storage, record a documented non-conformance, or reject `USAGE BIT` loudly:
**implement in full.** Landed as **D19** in `COBOLNET_DATA_MODEL_DESIGN.md` (fix-queue PB43) — the §8.5.1.6.3
layout, a packed record image, and `LENGTH`/`BYTE-LENGTH` agreeing with both.
⛔ **THE LESSON IS ABOUT DESIGN DOCS, NOT ABOUT BITS.** D-B1 justified char-per-bit storage with §13.18.40.4 GR14
("a boolean character can be represented … as a bit, an alphanumeric character, or a national character") and
called the choice "PERMANENTLY conforming". GR14 lists the AVAILABLE representations; **§13.18.60.4 GR5 — "the
USAGE BIT clause specifies that bits SHALL be used" — SELECTS one**, and §13.18.60.3 SR13(b)+GR7 select the other
when no USAGE clause is written. A design doc citing a real clause that does not govern is harder to catch than a
wrong citation, because `cite.py --check` passes. **The word to distrust is "permanently".**
⚠ Plain `PIC 1(n)` is untouched and its char-per-position storage is REQUIRED, not merely licensed — so a program
that writes no `USAGE BIT` is unaffected, and that is proven structurally: the bit walk is reached only for a
subtree with a `USAGE BIT` leaf, and without one it agrees with the old character sum by construction.

**⚙ 2026-08-04 SESSION CLOSE — PB17 LANDED IN FULL, and it dragged two more defects out with it (PB41, PB42).**
⛔ Run the probe; never quote a number from this paragraph. `kb/Work/` says what is open — not this section.
**▶ THE D18 ROUTE IS NOW THE STANDING ANSWER for any subscript / ref-mod segment the token renderer cannot
render**: re-parse the verbatim text through `subscriptExpressionFragment : arithmeticExpression EOF` and bind it
through the ONE `ExpressionBinder.BindExpr`. **The gate is "can the renderer render it", NOT a token list** —
PB42 is what the list version cost (`W-E(W-I ** 2)` and `W-E(2.0)`, plain arithmetic, still throwing at run time
one commit later). The `arithmeticExpression` grammar adjudicates admissibility, so the next arithmetic form needs
no edit at all.
⚠ **THE OPEN RESIDUE THIS WAVE LEAVES**, both recorded on their notes and neither an oversight: the three
per-evaluation windows `UdfStagePerEvaluationResidue` stages loud (`COBOLNET1509`) now reject function-bearing
SUBSCRIPTS in those positions as well as user-function calls; and an alphanumeric-literal or `ALL` subscript is
correctly refused but at RUN TIME rather than as a bind diagnostic.
⛔ **A CORRECTNESS LESSON WORTH MORE THAN THE FIX: the §15.4 temp's own DESCRIPTION was a latent wrong answer.**
D18 had specified `Scale: 0`; that truncates, so `W-E(FUNCTION SQRT(2))` would have silently indexed occurrence 1
instead of setting EC-BOUND-SUBSCRIPT. Asking what that would do is what exposed PB41 — the same bug with no
function in sight. When a design names a synthesized item's PICTURE, ask which spec fact that PICTURE destroys.
⚠ **A PROCESS GUARD LANDED 2026-08-04 AND IT IS MECHANICAL, NOT ADVISORY:** `scripts/hooks/fleet_active_build.py`
DENIES `dotnet build`/`test` while any subagent transcript for the session was written in the last 120 s. It exists
because I rebuilt the compiler ~6 times underneath a running 60-agent measurement fleet, wasting all of it —
and explained away the `MSB3027 cobol.exe locked` build failure that was the tell. Measure INLINE first; a fan-out
for work already done is pure waste.

**⚙ 2026-08-04 (later session) — PB15 LANDED, and the subject was an ARCHITECTURAL claim that had been false for
a year.** ⛔ Run the probe; never quote a number from this paragraph.
`COBOLNET_INTRINSICS_DESIGN.md` **D2** says the catalog "is the single source of result-category truth" — it was
not. **Three** mechanisms decided the property: the scalar `IntrinsicType` column plus **two hand-written name
lists** in `IntrinsicBinder` (CA25's three names, V54's two). ISO §15 gives **twenty** functions a type that
depends on their arguments, so the ten nobody had a list for stayed mislabelled. The rule is now a catalog
COLUMN (`IntrinsicResultRule`) with **one** reader, and `IntrinsicResultTypeDriftTests` **re-derives the
population from `specs/ISO_COBOL.md` itself**, so the next such function fails the build instead of being missed.
⭐ **THE TRANSFERABLE RESULT IS THE SAME ONE AS THE LAST TWO SESSIONS, EARNED AGAIN: MEASURE THE ENTRY.** PB15
named four functions; eight were broken. It was filed `wrong_answer`; the run-time value was correct in every
case and the defect is a silent **under-rejection** — a different defect needing a different fix.
⚠ **AND MEASURING THE FIX'S OWN CLOSING CLAIM OPENED PB40.** The note said the new INTEGER-following rules were
"not yet observable"; probing that produced `FUNCTION CHAR(FUNCTION ABS(<PIC 9V9>))` compiling clean against
§15.15.3 r1. The §15.3 screen resolves through CLASS and §15.2 puts integer and numeric functions both in class
numeric, so its `'i'` arm cannot distinguish them. **A claim about reachability is a measurement, not a
deduction.**
⛔ **THREE OF MY OWN ERRORS, EACH CAUGHT BY RUNNING SOMETHING:** a "sibling defect" I fixed and had to un-fix (a
group of `PIC N` children is an ALPHANUMERIC group — §8.5.2.10 item 3 needs `GROUP-USAGE NATIONAL`, which is not
modelled — so the compiler was already right); a citation `cite.py --check` rejected (§13.18.27 for GROUP-USAGE;
it is **§13.18.29**); and a new source-form guard that fired on **its own explanatory comment**, found only
because the guard was proven in the failing direction before being trusted.
⚠ **A METHOD WARNING AGAINST MYSELF:** I launched a measurement fan-out over this population and then rebuilt the
compiler underneath it, so its agents probed a moving binary and its refuters re-ran against an already-fixed
tree. It was stopped and **none of the numbers above come from it** — every one is a before/after run directly.
**Do not start a measurement fleet and then edit its subject.**

**⚙ WHERE THE 2026-08-03/04 SESSION ENDED — 27 commits, tree clean, pushed.** ⛔ **Run the probe; never quote a
number from this paragraph.** At close: denominator **3,861 → 3,981** · GAP **3,796 → 3,907** · corpus 247 →
**255** positive goldens · work register **67 items, 35 open / 3 half / 28 landed, 14 actionable**.

⛔ **THE BIGGEST STRUCTURAL CHANGE: THERE IS NOW EXACTLY ONE WORK REGISTER, `kb/Work/`, AND CLAUDE.md RULE 8
FORBIDS STARTING ANOTHER.** Five registers had accumulated — this §0's own NEXT table, `CONFORMANCE-FIX-QUEUE.md`,
`kb/Remaining Work Tracker.md`, the §11 analysis backlog, and the fix queue's never-itemised RESIDUE block — and
**three of them each declared themselves canonical.** The cost was measurable: a WRONG-ANSWER defect
(`EXCEPTION-STATEMENT` returns `GO` where Table 12 requires `GO TO`) sat inside a prose paragraph where no work
list could see it, and this section's own duplicate table had rotted into listing landed items as open.
**Ask the register; never re-derive a worklist from prose:**

| to ask | run |
|---|---|
| what should I work on now? | `python scripts/spec/work.py next` — session-probe prints it every session |
| show me everything | `kb/Work.base` → **Fix next** · **Blocked** · **Open but no harm flag set** |
| is the register sound? | `python scripts/spec/work.py check` |

⛔ **AND KEEP IT CURRENT IN THE SAME CHANGE SET AS THE WORK** — a landed fix flips its note's `status` in the
commit that lands it; a newly found defect becomes a note BEFORE it becomes a DEVLOG paragraph. §0 keeps live
state, gates, owner decisions and narrative. It does NOT keep a worklist, and must never regrow one.

**FINAL COMPREHENSIVE GATE on the landed tree: Conformance 4185/4185 · Unit 3639/3639**, both 0 failed 0 skipped;
the later PB24 waves re-gated the corpus (426/426) and the full Unit suite.
⭐ **THE WHOLE TWO-ARM-DISPATCH FAMILY IS CLOSED** — PB2 · PB13 · PB14 · PB28 · PB32 · PB38, six items, five of
which were the same sentence. **All three owner decisions were taken and applied.** PB33/PB34 landed; **PB24 is
three-quarters done** (ref-mod · variable-length group · PHYSICAL) with §15.50.4 r9 measured and VIOLATED.
⚠ **THE GAP WENT UP, AND THAT IS THE MODEL WORKING:** admitting §8.8.1, §14.7.5, §15.4.1 and the multi-sublist
clauses added rows while the fixes closed 9. Adjudication OPENS work; only Phase C closes it.

⭐ **THE SESSION'S ONE TRANSFERABLE RESULT, AND IT IS THE SAME ONE AS LAST TIME, EARNED AGAIN ON DIFFERENT WORK:
MEASURE THE ENTRY BEFORE IMPLEMENTING IT.** Both items were re-measured first and both changed:
· **PB32's SHAPE was right and two of its three named INSTANCES were wrong.** MOD's zero-divisor drift is real
  and worse than written (a FATAL condition never SET, execution continuing past it); MEDIAN's even-count mean is
  correct in BOTH arms; MIDRANGE's ×5 is a defect of the **exact** arm, not the float one. And the family's
  largest defect was filed as a **separate item** — PB14 and PB32's Dec leg are ONE defect recorded twice.
· **PB29's count was exactly right for its subject and 5× short overall** — §8.8.1 is 21 ordinals as it says; the
  same blind spot across the standard is **100 clauses / 460 ordinals**.

⛔ **AND THE HALF-FIX LOOKED LIKE A WHOLE ONE.** Landing the `Dec` arm in `NumericRenderer.Align` alone made
MAX / MIN / MOD / MEDIAN compile again while ABS, SIGN, INTEGER, FRACTION-PART and FACTORIAL still failed —
**the four that recovered are exactly what a spot-check samples.** The landing belongs at `IntrinsicRenderer.Arg`,
the single origin of every numeric argument. Measure the WHOLE population after a choke-point fix, not a sample.

**⛔ THE FIVE THINGS THE NEXT SESSION SHOULD KNOW, in priority order.**

0. **⚖ ALL THREE OWNER DECISIONS WERE TAKEN 2026-08-03 — none is open.**
   · **PB18 — native `**`: EXACT `Int128` while the result fits, the documented double approximation past it**
     (never a size error merely for outgrowing the carrier). ✅ **LANDED**, with PB28 and PB32's blocked half.
     Recorded in full as `COBOLNET_NUMERIC_DESIGN.md` **D19**. ⭐ The SURVEY decided it: IBM and Micro Focus fall
     back to floating point past the fixed capacity, GnuCOBOL has no boundary (GMP), so no shipping COBOL raises
     a size error there — which ruled out `CobolDec.Pow`'s own precedent.
   · **PB37 — NUMVAL-C's LOCALE phrase is an §A.4.9 OPTIONAL element** (editorial omission). ✅ **APPLIED:**
     eight rows (§15.68.3 r5 + the seven r5b items) are DOCUMENTED-NON-SUPPORT and now CLOSED, and the
     determination with its grounds is in `docs/CONFORMANCE.md` §4 item 5. ⭐ **The compiler and the doc were
     ALREADY behaving this way** — `COBOLNET1518` rejects the phrase at bind time citing §A.4.9, verified by
     probe, and `LocaleDispositionTests` already covered it. Only the inventory and the *justification* were
     missing, which is why this closed 8 rows without a line of compiler change.
   · **THE DENOMINATOR — adjudicate all 100 unharvested clauses as ONE BATCH and admit the normative ones.**
     ⚙ **STARTED: 15 clauses ADMITTED (120 rules), 85 clauses / 344 ordinals remain.** The whole MULTI-SUBLIST
     slice is done except §8.3.2.2. The denominator is now **3,981** (was 3,861) and §11 **A3 is UNBLOCKED** — it can no longer audit the
     intermediate-results model against a catalog omitting §8.8.1. The MECHANISM is data-driven: a clause is
     admitted by setting `disposition: "rules"` + `kind` in `spec-unharvested-rule-blocks.json`, never by adding
     its prose title to `KINDS` (that would be the hand-maintained list this project keeps deleting).
     ⛔ **THE SHAPE SIGNAL I WROTE HERE WAS FALSE AND IS RETRACTED (measured 2026-08-04, DEVLOG 1165).** It said
     nested sub-lists were weak evidence for a prose enumeration. **All ELEVEN multi-sublist clauses were read and
     every one is normative** — §14.7.5 DEFINES the size error condition and §15.4.1 r1 is "the returned value
     shall equal the value of the equivalent arithmetic expression". Multiple restarting sub-lists mean the clause
     states SEVERAL rules, each with its own lead-in, which the extractor already represents as
     `L<sublist>.<ordinal>`. **Shape records LIST STRUCTURE AND NOTHING MORE — expect the 460 upper bound to be a
     HIGH one, and READ THE CLAUSE.**
     ⚠ **AND MY OWN EXAMPLE WAS WRONG** — see DEVLOG 1162: I said §8.3.2.2 "User-defined words" lists kinds of
     word, reading the TITLE and asserting the BODY. Its 15 items are five nested sub-lists, several normative.
     The conclusion held; the reason did not. `docs/rearchitecture/spec-unharvested-rule-blocks.json`
     is the worklist (100 clauses / 460 ordinals, every entry `disposition: "pending"`). Each is judged either
     normative rules (admit, typed per §5.3) or prose enumeration (`not-rules`, with the reason). **460 is an
     UPPER BOUND**, and SHAPE is the triage signal: **89 clauses / 350 items are a SINGLE ascending list**
     (rule-block shaped) against **11 clauses / 110 items of nested sub-lists** (prose-enumeration shaped).
     ⚠ §8.3.2.2 "User-defined words" is in the SECOND group — 15 items in five restarting sub-lists; §14.7.5 and §8.8.1.2 are plainly
     normative. The denominator and therefore v1.0's meaning change when this lands. **§11 A3 unblocks with it.**

   ⛔ **AND THE PROCESS LESSON THAT COST THE MOST: I ASKED TWO DECISIONS, GOT COUNTER-QUESTIONS BACK, ANSWERED
   THEM — AND NEVER RE-ASKED.** One I then decided MYSELF on the owner's stated rule, which D13 forbids
   (`DOCUMENTED-NON-SUPPORT` is never an agent's to choose); the third I only mentioned in a summary and never
   put as a question at all. The owner's correction was blunt and correct: *"You never asked me for any
   decisions."* **A counter-question is not an answer — re-ask once you have what it wanted**
   (`feedback_ask_bare_decision`).

1. **⭐ MEASURE A PLAN STEP BEFORE EXECUTING IT — three of §5b's four premises were WRONG.** This is the
   session's most transferable result. Step 2's "cheapest closure" was **10 of 199** DOC obligations discharged,
   not "a large fraction already done"; step 3's "needs a per-STATEMENT key" was already true and the real gate
   was per-agent SIZE; step 4's premise held but **the grammar audit could not run at all**. Each measurement
   cost minutes and changed the work. §5b's own rows now carry the corrections.
2. **⛔ THE TWO-ARM DISPATCH — STRUCTURAL HALF LANDED; THE REMAINING HALF IS BLOCKED ON PB18.**
   Three choke-point changes landed (`1330cf5d`): **one raise site per RULE, not per carrier**
   (`ModZeroDivisor`/`RemZeroDivisor`) · **one SDIDI landing at `IntrinsicRenderer.Arg`** plus the `Dec` arm
   `Align` was missing while its own comment declared itself "TOTAL OVER THE CARRIER KINDS" · **the exact
   carrier's escape boundary raises `EC-SIZE-OVERFLOW` instead of wrapping**. `IntrinsicCarrierAgreementDriftTests`
   now asserts the two carriers AGREE (its sibling only asserted the second body EXISTS) and its general arm
   catches the shape rather than the instances.
   ⛔ **WHAT IS LEFT IS A CONTROL-FLOW DEFECT AND IT NEEDS PB18 FIRST.** `NumericRenderer.Power` returns
   `Real: true` in a receiver-less context, so `FUNCTION MOD(A ** 2, B)` is **930000007 under `COMPUTE` and
   930000008 under `DISPLAY`/an `IF` subject**, and `IF FUNCTION MOD(A ** 2, B) = 930000007` evaluates FALSE — a
   function's value depending on its receiver's SHAPE, which §15.4 forbids and PB13 closed for the float family
   only. Its root cause is PB18 (`**` has no exact arm for an integer exponent), so **it closes when PB18 does**.
   Verified still reproducing after this wave landed.
   ✅ **AND PB38 — the last member — LANDED 2026-08-04.** `RenderNum` was the ONE renderer not testing the
   arithmetic mode before its float branch. The fix needed **no new selector and no Dec-carrier body**: the mode
   belongs inside the ONE landing PB32 built (`IntrinsicRenderer.Landed`), where a float now converts in through
   the compiler's own §8.8.1.5.1 `DecOperand` and lands exactly as a `Dec` operand does. ⛔ **The half that would
   have been missed:** `AnyRealArgument` had to be asked of the **LANDED** operand — reading the raw one leaves
   the dispatch on the binary64 body while the landing silently does nothing, i.e. the fix present, the defect
   intact and every test green. **The whole PB2 · PB13 · PB14 · PB28 · PB32 · PB38 family is now closed.**
3. **⚙ PB29 — DETECTOR LANDED (`24e9e117`); THE ADJUDICATION IS THE OWNER DECISION IN ITEM 0.**
   The extractor now scans by CONTENT, not by heading text: `unharvested_rule_blocks` reports every clause whose
   body carries an ascending `N)` list that nothing harvested, compared against the committed manifest
   `docs/rearchitecture/spec-unharvested-rule-blocks.json` (100 clauses / 460 ordinals, every entry
   `disposition: "pending"`). **`--check` fails on three drifts** — a clause newly carrying rules, one that
   stopped, a count that moved — and it was **proven by making it fail** before being trusted.
   ⭐ **WHY THE TWO EARLIER DENOMINATOR FIXES COULD NOT HAVE FOUND THIS:** both extended a HEADING-keyed guard,
   and §8.8.1.2 is titled "Native, standard-binary, and standard-decimal arithmetic". **A guard keyed on what a
   block is CALLED can never find a block called something else.** Reach for a content key next time.
   ⚙ **The owner decided; 7 clauses are ADMITTED, the denominator is 3,904 and §11 A3 is UNBLOCKED.**
   85 clauses / 344 ordinals remain `pending` — the rest of the batch, now all SINGLE-list except §8.3.2.2.
   ⚠ **§8.3.2.2 IS PENDING FOR A MECHANISM REASON, NOT DOUBT:** its five sub-lists are normative but NOT ALL THE
   SAME KIND (two constrain where words may be WRITTEN ⇒ SR; two state effect ⇒ GR), and the manifest carries ONE
   `kind` per CLAUSE. Admitting it needs per-SUBLIST kinds — a manifest + extractor change. First of the 100 to
   need one; there will be others.
   ⛔ **AND ADMITTING §9.3.8.2.3 EXPOSED PB39** — 326 catalog rule-ids (8%) do not match the standard's own
   numbering, because a nested list mis-segments the top-level run. The COUNT is right, the IDENTITY is not.
   ⭐ **AND THE EXTRACTOR NOW REPORTS ZERO PARSE GAPS, for the first time.** The `RV §15.4 (Returned values)` gap
   that §0 carried for months as "PRE-EXISTING and unowned" is EXPLAINED, not suppressed: §15.4's rules live in
   the **§15.4.1** sub-clause, whose prose heading `KINDS` does not recognise — PB29's defect class one level
   down. §15.4.1 is admitted (6 RV rules, including the r1 this session cited repeatedly while it sat outside the
   denominator) and §15.4 is a DECLARED empty block carrying that reason.
4. **THE REFUTE STAGE IS NOW MEASURED, NOT ASSUMED: five batches, ~16 overturns in batch 5 alone, EVERY overturn
   a downgrade.** The two mechanisms are the playbook's own — "the adjudication SAMPLED OUTPUTS" and "read ONE
   of the TWO bodies". Never drop it to go faster; an adjudicator's CONFORMS is worth what its refuter leaves.
5. **⚠ CI RUNS ON LINUX AND THIS SESSION BROKE IT ONCE.** A "cross-platform" test helper built by STRING
   SUBSTITUTION left `exit /b 7` for POSIX `sh` (rc=2, not 7). `feedback_wsl_linux_repro` now names the TRIGGER:
   **writing any per-OS branch**, not seeing a red. Build on Windows, run under WSL (`/mnt/e/CobolSharp`,
   `~/.dotnet/dotnet`, `--no-build`) — 2 minutes against a ~30-minute round trip.

**BATCH MECHANICS THAT CHANGED — use these, the old commands mislead.**
· `phase_b_batch.py --max-rules` (default 20) splits an oversized subject at CLAUSE boundaries; §14 = 90 agent
  files, §13 = 99. A subject that FITS is not split, so §15's proven shape is unchanged.
· Write batch outputs to a **batch-specific directory** (`scratchpad/phase-b/b<N>/`) and merge **BY NAME**.
· Run `scripts/spec/normalize_batch.py <files> --in-place` BEFORE `record_verdicts.py`: agents append commentary
  to `test-ref`/`code-location` and write `Foo.cs#Foo.Bar` where the file already names the class. It reports
  every change and preserves rather than deletes.
· Then the gate: `dotnet test tests/Cobol.Net.Tests.Unit --filter "FullyQualifiedName~SpecTraceabilityInventory"`.


**⚙ 2026-08-03 — THE INSTRUMENT WAVE (§5b step 1). The harnesses were fixed BEFORE any more of their output was
believed.** No compiler behaviour changed; every change is to a gate, a guard or a test harness.
**ONE ROOT CAUSE, five harnesses: A MISSING OBSERVATION WAS BEING READ AS A NEGATIVE OBSERVATION.** §11
**A12 · A12b · A12c · A12e are CLOSED**; **A12d's STRUCTURAL half is done but its DISTRIBUTION is NOT collected
— that row is NOT closed**, and the honest reason is in it. New: **A13** (battery concurrency; half done). The rule now written down once and
implemented everywhere is **DESIGN-test-build-ci.md §3.10, the VERDICT-EVIDENCE INVARIANT**: a verdict about the
compiler is produced ONLY from an observation actually made — accept needs its artifact, reject needs its
diagnostic, MATCH/DIFF needs a run that finished — and anything else is a LOUD `NO-VERDICT`, never a silent pass
and never a manufactured failure. Every iterating harness also asserts that its results are a **PARTITION of its
declared population**, compared against a COMMITTED MANIFEST rather than a remembered number.
⭐ **Two things that generalise beyond this wave.**
· **The same fifteen lines existed SEVEN times** (start `dotnet`, wait N s, return whatever came back) across five
  test projects, every copy handing back TRUNCATED output on a timeout for the caller to compare against a golden.
  They are now one `tests/_shared/ProcessObservation.cs`, and `ProcessObservationDriftTests` — which found the
  seventh copy on its first run — keeps them collapsed.
· **A hand-maintained list lost to the data it was approximating, twice.** `guard-fast`'s "these six suites run
  serially" grouping is replaced by the DECLARED chain graph in `corpus.tsv` (longest serial group **40 → 9**);
  the hand list had also over-grouped `SQ204A`, which creates its own file. And this audit's own first draft used
  a hand-written list of `WaitForExit(` numeric prefixes that would have blessed `WaitForExit(45000)` silently.
⚠ **AND MY OWN FIRST FIX WAS WRONG IN THE OWNER'S DIRECTION:** I capped the guard's fan-out to halve contention,
which trades throughput on EVERY run to protect against something the evidence rules now DETECT. Reverted — full
fan-out is kept and only the LOST observations are re-taken serially. **Tests run as concurrently as correctness
allows; reduce the WORK, not the parallelism.** `scripts/battery.sh` now runs Conformance ∥ Unit ∥ Characterization
concurrently (§11 A13; §0's "one leg at a time" caution is specifically about a REBUILDING leg overlapping a
`--no-build` one, not about two `--no-build` assemblies).

**⚙ 2026-08-03 (same session, after the instrument wave) — §5b STEPS 2, 3 AND 4 WORKED, AND THREE OF THE FOUR
PREMISES WERE WRONG.** That is the headline: §5b was written from reasoning, not measurement, and measuring it
first — before doing the work it described — changed the work every time. **Measure a plan step before executing
it; the measurement is usually cheaper than the step.**
· **Step 2 (the 222 DOC rows) — premise REFUTED.** Ranked "the cheapest closure in the whole inventory" on the
  belief that a large fraction were already documented. Measured: **10 of 199 obligations discharged, 189 remain**.
  The "large numbered set" it meant is CONFORMANCE.md §2 (Annex A.3) and §3 (Annex E) — different registers.
  It also found §7 filing the §15.3.3.2 determination under **item 87**, which is FORMATTED-CURRENT-DATE; it is
  **item 202**. `scripts/spec/audit_annex_a1.py --check` now re-derives every number and is proven to fail on it.
· **Step 3 (a per-STATEMENT batch key) — premise REFUTED.** The key was ALREADY per-statement/per-clause. The real
  gate is per-agent SIZE (§15's proven max is 18 rules; SET carries 98). `phase_b_batch.py --max-rules` splits at
  clause boundaries, leaves a fitting subject whole, and asserts the partition loses nothing.
· **Step 4 (unify the grammar audit with FMT+SR) — premise HOLDS**, the only one that did. But the audit **could
  not run at all**: de-paging removed the catalog's `page` field and its section lookup matched exactly, so its
  own example args selected zero rules SILENTLY. Fixed (`clause_page.py`), then unified —
  `grammar_findings_to_batch.py` turns one pass into both a grammar report and a verdict batch, and **refuses to
  expand a finding across rules nobody examined**.
⛔ **AND CI WENT RED ON MY OWN INSTRUMENT-WAVE COMMIT** — one test, ubuntu only, Windows green. A "cross-platform"
helper built by STRING SUBSTITUTION (`>nul`→`>/dev/null`) left `exit /b 7` for POSIX `sh`, which returns rc=2.
`feedback_wsl_linux_repro` exists for exactly this and I did not apply it when writing the file. **Write per-OS
commands explicitly; run under WSL before pushing** — 2 minutes against a 30-minute round trip. ⚠ Note the two
sibling tests that PASSED on Linux with Windows-only `ping -n 30` syntax, purely because iputils keeps running:
green for the wrong reason, inside the wave's own guard.

**⚠ PREVIOUS SESSION (2026-08-02/03, head `cf7887a7`) — nine commits: PB13 · the `'i'`/`'n'` adjudication · PB10 ·
PB11 · Phase-B batch 4 · PB20 · PB19 · PB21 · PB22.** No BLOCKER is open. Corpus 242 → 247 positive
goldens, 145 → 154 negative fixtures. Inventory 253 → **330 adjudicated**, GAP unchanged at 3799 — adjudicating
OPENS items, fixing CLOSES them, and that session did both.

**⚖ ONE OWNER DECISION WAS TAKEN AND IS NOW LAW (2026-08-02):** a **numeric-edited item is NOT an arithmetic
operand and NOT an integer argument**. §8.8.1.1 admits "an identifier referencing a NUMERIC data item"; §8.5.2.13
calls this a "numeric-edited data item", a distinct defined term; and §15.43.3 r1 shows that when the standard
means to admit one it says "category numeric **or numeric-edited**" explicitly, which §15.3's integer and numeric
types do not. De-editing is granted by the MOVE rules (§14.9.25.4 GR5/GR6d1) and nowhere extended to arithmetic.
Both external oracles agree. **Do not re-argue this** — it was argued three times; DEVLOG 1142 and
`pb1-integer-arg-numeric-edited` carry it.

**⛔ FOUR THINGS THAT WILL BITE THE NEXT SESSION, each earned today:**

1. ~~**A GREEN `guard-fast` IS NOT EVIDENCE until §11 A12c/A12d/A12e close.**~~ ✅ **CLOSED 2026-08-03 — see the
   INSTRUMENT WAVE block below.** The defect was real and is described in §11 A12c: `=== ALL GREEN ===` at
   **352 MATCH against a 353 baseline** because `SQ135A` vanished and nothing asserted the POPULATION; five runs
   on one unchanged tree gave five outcomes; the GnuCOBOL differential scored a case `WE_REJECT_THEY_ACCEPT` with
   `ourCodes: []` and an EMPTY error string. **One root cause: A MISSING OBSERVATION WAS BEING READ AS A NEGATIVE
   OBSERVATION**, and it is now closed at every verdict site. ⚠ **THE HABIT SURVIVES THE FIX: attribute every red
   MECHANICALLY** (can the change even reach that program?), never statistically.
2. **⛔ NEVER MERGE A PHASE-B BATCH WITH `record_verdicts.py … out-*.json`.** The playbook's own documented glob
   sweeps EVERY prior batch's output out of the shared directory: at batch 4 it offered **144 records instead of
   77** and would have RE-ADJUDICATED PB11's freshly-closed rows BACKWARDS from files written before PB11
   existed. **The tell is the GAP going UP in the `--dry-run`.** Merge the batch's own files BY NAME.
3. **A QUEUE ENTRY'S SUMMARY IS A CLAIM — three were wrong today.** PB13 cited **§14.7.4** (the ROUNDED phrase)
   where the rule is **§14.7.5 case 5**; PB13's recipe said the fix "cannot be emitter-side" when it is entirely
   emitter-side; PB11's said "argument-3 in [0,86400)" when r4 states no range at all and the bound comes from
   the **§7.3.17 LEAP-SECOND directive**. Re-derive the citation before building on it.
4. **THE WORST DEFECTS RETURN A PLAUSIBLE ANSWER, NOT A BROKEN ONE.** PB5 returned `9223372036.85` for a money
   value; PB13 returned `0170141183460469231731687303715`; PB22 returned **143951, a genuinely valid integer
   date**, because 2⁶⁴ + 1995046 wraps to 1995046. **Sampling outputs cannot find these** — read the
   implementation. That is why the Phase-B refute stage keeps finding what the adjudicators looked straight at.

**⚠ A REGRESSION WAS INTRODUCED AND CAUGHT WITHIN THE SESSION, by the review's own refute stage.** PB13's
receiver-less arm bypassed `FromDouble`, which was the EC-ARGUMENT-FUNCTION raise site, so
`COMPUTE R = FUNCTION ACOS(2)` gave the §15.3 default 0 while `IF FUNCTION ACOS(2) = 0` propagated a raw NaN —
and under checking it raised nothing at all. Fixed by `CobolIntrinsics.RealResult`, pinned by
`pb13_domain_raise_receiver_shape`. **A function's returned value must not depend on the SHAPE of its receiver**
(§15.4), and PB26 below is the general form of that same defect.

**⚠ TWO GUARDS WERE FOUND BLIND, AND BOTH HAD BEEN GREEN FOR MONTHS.** `IntrinsicRealArgDriftTests` exempted the
exact group its defect lived in (an `'i'` row DOES admit a float — §15.3's integer type resolves through class
NUMERIC) *and* its case-label regex captured only the first name of an `or`-chain, so "three missing members" was
really **ten**. When a guard is green, ask what it LOOKED AT. Make a guard fail once before trusting it — that is
now done for `FloatQuantizeHeadroomDriftTests`, `RefModCategoryDriftTests` and the PB20 citation guard.

**⚠ AND A SCREEN IS ONLY AS GOOD AS ITS CLASSIFIER.** PB19's rows were right and still rejected legal source,
because `ClassOf` flattened every `BoundStringLiteral` to alphanumeric and ignored the `Category` it carries.
PB1's disaster was unaudited ROWS; this was an audited row over a lossy CLASSIFIER, one layer down and invisible
to any review of the rows.

**⛔ BEFORE PICKING THE NEXT ITEM, RUN `python scripts/spec/work.py next` — THE WORKLIST IS `kb/Work/`, NOT §0.**
§5b below is the CAMPAIGN plan for v1.0 and changes what "next" should mean; it is strategy, not a worklist. Its three load-bearing
measurements: **3,531 of 3,861 rows are unadjudicated (91 %)** and 327 of the 330 done are §15 — the review has
driven the narrowest vein; **GR + SR are 2,865 rows (74 %) at ~0 %**; and **only 19 % of adjudicated rows CLOSED**,
so adjudication maps the territory while only Phase C shrinks the GAP. It also names the single largest efficiency
win available — **item 3's grammar audit (1,659 items) and the inventory's FMT+SR (1,674 rows) are the same body
of work counted twice**, and unifying them before the SR mass starts avoids auditing ~1,670 rules a second time.

### NEXT, in order

1. **⛔ START HERE — THE ROAD TO v1.0 IS THE PHASE-14 STEP-0 TRACEABILITY REVIEW, AND IT NOW RUNS ON TWO TRACKS.**
   `pwsh scripts/session-probe.ps1` reports the live GAP against the catalog — **v1.0 is defined as ZERO GAP**,
   and the denominator is **3,861** (never quote a GAP number from this document; the probe computes it).
   This is item **5** below (the FULL implementation↔spec review); it is the top of the list, not the bottom.
   **Read `docs/rearchitecture/DESIGN-spec-conformance-review.md` before starting** — §4 is the row schema and
   §8 is the recording mechanism; the inventory enumerates every rule and drives it to zero, four editions wide.
   ⛔ **TWO SOURCES OF WORK INTERLEAVE, AND BOTH ARE LIVE.** *Adjudicate* the next clause (grows the map, opens
   items) and *fix* what earlier batches found (shrinks it). `docs/rearchitecture/CONFORMANCE-FIX-QUEUE.md` is
   the defect register and its header is the tally; **read `DESIGN-spec-conformance-review.md` §9 before running
   a batch** and **§4/§8 before recording a verdict**.

   **THE NEXT BATCH is `python scripts/spec/phase_b_batch.py 15.20-15.31`** — ⚠ verify against the inventory
   rather than this line; §15.7, §15.8–15.19, §15.32–15.44, §15.45–15.57, **§15.58–15.69 (batch 5, 2026-08-03)**
   and §15.70–15.79 are done. **Batch 5: 90 rules, 12 subjects, 24 agents, 0 errors — DIVERGES 42 · PARTIAL 21 ·
   NOT-IMPLEMENTED 15 · CONFORMS 11 (3 closed) · NEEDS-OWNER-DECISION 1; inventory 330 → 420 adjudicated, GAP
   3799 → 3796. Its 79 open findings cluster into EIGHT causes (fix-queue PB30–PB37), and ~16 refute-stage
   overturns were ALL downgrades** — for the two reasons the playbook names: the adjudicator sampled outputs,
   or read one of two bodies. A batch is: generate one input file per subject → one
   adjudicating agent and one INDEPENDENT refuting agent each → `record_verdicts.py --dry-run` → merge → gate.
   ⛔ **MERGE THE BATCH'S OWN FILES BY NAME, NEVER `out-*.json`.** The playbook's documented glob sweeps EVERY
   prior batch's output out of the shared directory: at batch 4 it offered 144 records instead of 77 and would
   have RE-ADJUDICATED PB11's freshly-closed FORMATTED-* rows BACKWARDS from 07-30 files written before PB11
   existed. **The tell is the GAP going UP in the `--dry-run`** — which is the whole reason that flag exists.

   **⛔ START HERE NEXT SESSION — the three live fronts, in value order.**
   1. **THE DENOMINATOR BATCH — 95 clauses / 439 ordinals still `pending`** in
      `docs/rearchitecture/spec-unharvested-rule-blocks.json`. The owner authorised admitting the normative ones;
      §8.8.1's five are done as the worked example (mechanism, citation pattern and `why` text all in that file).
      **Shape is the triage signal:** 89 of the 100 are a SINGLE ascending list (rule-block shaped), 11 are nested
      sub-lists. ⚠ Weak evidence, not proof — §8.3.2.2 taught that the hard way (DEVLOG 1162). Admitting is a
      DATA edit (`disposition: "rules"` + `kind`), never a code edit.
   2. **§11 A3 — the numeric-semantics depth audit — is UNBLOCKED and its worklist exists:** 20 open §8.8.1 GR
      rows (the 21st, GR-8.8.1.2-6, is already CONFORMS). It was blocked precisely on those rows existing.
   3. **The fix queue below**, now that the two-arm family is closed. ⚙ **PB33's digit-cap half + PB34 landed
      2026-08-04 and were ONE defect measured as three** — all three NUMVAL validators enforced the cap and all
      three value producers returned an `Int128` saturation artifact while execution continued past a FATAL
      condition. **PB33's other half (§15.68.3 r1's two general formats) is still open** and is the natural next
      item; after it, PB12 · PB15 · PB17 · PB23–PB27 · PB30 · PB31 · PB35 · PB36 + the 16-finding residue.

   ⛔ **THE OPEN QUEUE LIVES IN `kb/Work/`, NOT HERE, AND THIS SECTION MUST NEVER REGROW ONE.**
   A worklist table used to sit at this spot. It was a SECOND register beside the fix queue, and it rotted
   exactly as a duplicate must: on 2026-08-04 it still listed PB18 and PB38 as open when both had landed, and
   PB28 as blocked on a decision already taken. **Everything it held is now one note per item under `kb/Work/`.**

   | to ask | run |
   |---|---|
   | what should I work on now? | `python scripts/spec/work.py next` — session-probe already prints it |
   | show me everything open | `kb/Work.base` → **Fix next** · **Blocked** · **Open but no harm flag set** |
   | is the register sound? | `python scripts/spec/work.py check` |
   | counts by kind/status | `python scripts/spec/work.py stats` |

   **`Fix next` = `not landed AND (wrong-answer OR crashes) AND not blocked`** — ranked by what a defect DOES to
   a user's program, never by its severity label. PB24 (`FUNCTION LENGTH` silently wrong) and PB39 (rule-id
   numbering, zero wrong answers) are BOTH `[MAJOR]`, and a session picked PB39. Severity cannot separate them.
   ⛔ **Repros for the batch-4 findings:** `docs/rearchitecture/evidence/PHASE-B-15.32-15.44-findings.md`.

   **⚠ FOUR STANDING CAUTIONS, each earned by a defect and each outliving the item that produced it.**
   · **A SHARED-GRAMMAR CHANGE MUST BE ADDITIVE UNTIL P15.** Collapsing a rule to a shared one deletes the
     generated `.dataReference()`/`.literal()` accessors and breaks the LEGACY compiler, which shares
     `CobolParserCore.g4` until the cut-over. Add an alternative; unify at P15, when legacy is deleted not migrated.
   · **A QUEUE ENTRY'S "ROOT CAUSE, ALREADY LOCATED" IS A CLAIM.** PB8's named a lexer defect and budgeted the
     riskiest category; a token dump showed the lexer was never involved. PB9's said "measured scope: one word"
     and the real answer was four — measured over the wrong population. Re-measure before budgeting.
   · **RUN THE FINDING'S OWN REPRO, NOT YOUR SUMMARY OF IT.** A weaker repro of PB13 missed the clamp entirely
     and nearly filed the blocker as a false positive.
   · **THE BATTERY IS NON-DETERMINISTIC UNDER LOAD** — two full Conformance runs on an identical tree gave
     4159/4160 then 4160/4160. Re-run a named red serially before believing it, and never accept "flake" without
     a mechanism. Registered as **§11 A12** (the audit) + **§12 R-6** (the risk); until A12 closes, a green
     battery is necessary-but-not-sufficient evidence for a conformance claim.

1b. **The two SMALL residues left behind on purpose**, both ledgered where they belong rather than lost:
   · `V59ImagePredicateDriftTests` pins a CLOSED inventory of ONE remaining `IsCharacterImage` use —
     `MoveEmitter.cs:144`, which is a strategy fast-path and **not** a guard. It must NOT be "migrated"; the test
     carries that note so a future session cannot do it by accident.
   · DA7's ledger names three further WRONG-STAGE neighbours of the same family (a correct verdict delivered at run
     time instead of compile time). None rejects legal source, so none is urgent.

> **The V59 execution record that used to sit at item 1 has been REMOVED from this list — it is finished.** Its
> reasoning (the one-width invariant, the byte-form discriminator, the record-image codec, the re-base, the support
> diagnostic, the §4.2.16 documentation, the drift golden) is in DEVLOG 1095–1102, and the on-disk form it pins is a
> documented user-facing guarantee in `docs/CONFORMANCE.md` items 205–215. §0 states WHERE WE ARE, never how we got
> here.

2. **SPEC RECONCILIATION — ⛔ PAGES ARE GONE FROM THE TRANSCRIPTION; it is CLAUSE-STRUCTURED and PUBLISHED.**
   - **`specs/ISO_COBOL.md` no longer has page anchors, `## Page N` headings or running headers** (owner
     directive: pages are not a thing in Markdown). 1,260 anchors · 1,260 page headings · 1,248 running headers
     removed; **zero content words lost.** Every cross-reference is now an intra-document link: the TOC is 896
     pure section links, and the index's 3,243 page references became CLAUSE links (52% exact — the term is a
     clause title or was located on the page; 46% page-level approximations, stated in a note at the head of
     the index; 2% keep their printed number as plain text). **3,720 section links, zero dangling.**
   - ⛔ **Anything page-keyed must be re-keyed onto the clause hierarchy.** `verify_publishable.py` and
     `verify_acknowledgment.py` are done (they delimit on clause 1 / the Introduction).
     `audit_figure_text.py`, `audit_underlining.py` and `audit_figure_structure.py` now **HALT LOUDLY** rather
     than report a false clean — for figures they are superseded by `sweep_figures.py --check`, which is exact.
     `extract_rule_catalog.py` halts too: it stamps a page on every rule and would record 0 for all of them;
     the existing catalog stays valid until it is re-keyed.
   - The reconciliation itself: 1,261 pages compared, 210 confirmed defects, 0 unverified. Figure classes
     closed by construction; **all 5 normative non-figure defects closed**; the structural classes are down to
     a residue dominated by TOC/index items that de-paging retired.
   - **⛔ THE PDF WAS NEVER OBFUSCATED.** 16 of 26 fonts were Identity-H subsets carrying **no `/ToUnicode`
     CMap**, so extractors emitted raw glyph indices (which print as Greek). Recovered by matching glyph OUTLINES
     against the stock Windows fonts and injected. The standard now extracts, copies and **greps**, with the
     publisher's bytes preserved verbatim (incremental save, 34 KB delta, zero pixel change over 53 hashed
     pages). Regenerate: `scripts/spec/pdf_deobfuscate.py --write out.pdf --verify`. Write-up:
     `spec-reconciliation/PDF-TEXT-LAYER.md`.
   - **MEASURE the page; never squint at it.** `figure_geometry.py` reads bracket / choice-indicator-bar
     rectangles · `figure_extract.py` reads which WORDS are underlined · `audit_underlining.py`,
     `audit_figure_text.py`, `audit_figure_structure.py`, `audit_grammar_optional_words.py` run those
     whole-standard · `render_figure.py` regenerates a figure from measurement.
   - **Four classes swept whole-standard, all essentially clean:** choice indicators **30/30** · underlining
     **0 defects in 2,215 tokens over 694 pages** · figure words **1 finding in 15,625 tokens over 820 pages**
     (and that one is ISO's own typo) · figure STRUCTURE 46 of 161 pages flagged, an **upper bound** dominated by
     layout conventions, not a defect count.
   - **⚠ MY CHECKERS WERE BUGGIER THAN THE TRANSCRIPTION.** The figure-text audit went 76 findings → 1 as three
     separate tool bugs came out; the underlining audit accused two CORRECT pages. **Confirm every measured
     "defect" against the raw rectangles before changing anything.**
   - **PUBLISHED.** `specs/ISO_COBOL.md` is in the public repo under the page-28 grant, opening with a Preface
     carrying ISO's acknowledgment verbatim and closing with an **Addendum** listing every correction beside the
     printed form so each is reversible. Gates: `verify_acknowledgment.py` (needs the PDF) and
     `verify_publishable.py` (deliberately does NOT, so it runs in the public repo).
   - **⛔ FIDELITY OF TEXT IS NOT LEGIBILITY, and only one gate measures the second.** Every audit here compares
     the file's TEXT to the printed page; all of them passed while the document rendered as a wall of italics
     (137 unescaped literal `*`, each opening emphasis that never closed) and every brace rendered as a dotted
     column (bare `<pre>`, so box-drawing rows sat 1.45 em apart and never met). Both were found by the OWNER
     opening the file. `lint_rendering.py` now covers that class — unbalanced emphasis outside code, `<pre>`
     without `line-height:1`, RUN-ON LISTS, a column header repeating inside a table body, ragged rows,
     caption-as-heading, unbalanced tags, dangling links. It needs no PDF, it is currently CLEAN, and it
     reports **767 defects** on the revision before those fixes, which is the evidence it can fail.
   - **The front matter and the index were never written as LISTS.** Markdown joins consecutive lines into one
     paragraph, so the index's 3,123 lines, the Figures list and the TOC each rendered as a single run-on
     block. All three are nested lists now. The index's sub-entry LEVELS are measured off the printed page
     (`measure_index_levels.py` → `data/index-levels.json`, committed so `relist_index.py` works without the
     PDF); measured and inferred levels agree 2,506/2,506. The lists of tables and figures are GENERATED from
     the body captions, which is what stops them drifting — 12 of the 15 figure entries had pointed at the
     wrong clause, because figure numbering and clause numbering share the annex letter and whatever built
     them matched on the number alone.
   - **Every page reference is now a clause link — none is left as a bare number**, including the 48 the
     de-indexer had skipped and the one normative citation in prose. The preface states the de-paging decision
     explicitly; previously it was documented only inside the index.
   - **758 horizontal rules removed.** A page boundary was transcribed as anchor + running header + a pair of
     `---` separators; de-paging took the first two and left 769 of the third, 287 of them in pairs with
     nothing between. Residue, not a convention: of 2,691 headings, 2,380 have no rule before them. The front
     matter keeps its 11 (they divide title-page blocks printed on ONE page). `strip_page_rules.py`.
   - **✅ THE TRANSCRIPTION HAS NOTHING OUTSTANDING — `lint_rendering.py` is CLEAN.** Every Annex D
     illustration is drawn (D.1, D.3, D.6, D.7, D.9–D.14) or deliberately left (D.2, D.4 were already
     correct; D.5 is Markdown tables, a fair representation of a class hierarchy). THREE GENERATORS, each
     with the printed folio in its docstring:
     · `repairs/annex_d_flowcharts.py` — the VARYING charts and D.1 (boxes down an axis, loops, second column)
     · `repairs/annex_d_truth_charts.py` — the condition-evaluation charts D.7–D.10
     · `repairs/annex_d_structure.py` — the two that are STRUCTURE, not flow: D.3's nested schematic and
       D.6's page layout
   - **D.6 added two rules that generalise** (both in `spec-reconciliation/TRANSCRIPTION-STATE.md`): a `<pre>`
     is raw HTML, so a figure's own `<blank>`/`<Detail lines>` notation is a TAG that a sanitizing renderer
     DROPS — escape at write time, never on the canvas, and `lint_rendering.py`'s new SWALLOWED check gates
     it (14 findings on the unescaped form). And **vertical distance can be the content**: D.6 stood as a
     Markdown table, which gives every row equal height and so erased both of the standard's meaningful voids
     (mid-body "and further body groups"; logical→physical bottom of form). Rows are placed from the measured
     printed y at the page's own 8.7 pt pitch; words conserve exactly, 150/150 against the printed page.
   - **⛔ EVERY ONE OF THESE GENERATORS NEEDS THE COLLISION GUARD** — `put` refusing to overwrite a non-blank
     cell, with a separate `junction()` for the one legitimate overwrite. It caught SEVEN defects that would
     each have rendered as a plausible picture (a branch drawn through a box wall, a loop through a border, an
     arrow terminating in blank space, two labels merging into `Fromtotherucomp.tgroup`). I omitted it when
     writing the third generator and it produced garbage immediately. Do not write one without it.
   - **The standard's long tables break across printed pages; the transcription had kept the breaks.** Table 13
     was five tables, Table 21 nine, Table 12 three, Tables 1/6/10 two each — each restarting under a repeated
     caption and a repeated column header that then sits in the body as a data row. 18 joints merged. Table 10's
     24x24 picture-symbol precedence matrix was rebuilt from the printed GEOMETRY (every cell is positional, so
     reading order silently shifts the marks left); it is verified square, symmetric in its symbol lists, and
     equal to an independent unsnapped mark count off the page — 163 = 163. **All 124 Markdown tables now have
     zero ragged rows.**
   - **✅ THE RECONCILIATION IS CLOSED at 210/210 — nothing outstanding.** The last three batches
     ("lost outer brackets", the residual underlining findings, "anchors/TOC folios") were closed by a CHANGE
     OF MECHANISM rather than item by item, which is why they can read as unworked: figures are now GENERATED
     from measured page geometry, so bracket/brace/underline classes cannot survive; and pages were removed,
     so the `#page-NNN` links those findings were about no longer exist. Repairing either individually would
     have been repairing an input nothing reads. Order and mechanism: `spec-reconciliation/REPAIR-PLAN.md`,
     whose header now records this.
   - **Batch 1's exit criterion is PROVEN and yielding — SR1 and SR2 in the fix queue.**

3. **GRAMMAR ↔ SPEC AUDIT (owner-directed, systematic) — STARTED, and the first vein is OPTIONAL WORDS.**
   **1,659 items: 321 general formats (432 numbered Formats) + 1,338 syntax rules.** Each divergence carries the
   EXACT ISO syntax its fix implements and becomes a fix-queue bug against the `.g4`.
   - **⛔ ROOT CAUSE FOUND (SR2): "unbracketed" was being used as the test for "required word".** §5.2.2/§5.2.3
     make **UNDERLINING** the test; bracketing marks whether a PHRASE may be omitted, not whether a WORD must be
     written. One wrong criterion, five sites. Landed so far: `ON` in SIZE ERROR / ON EXCEPTION, `FROM` on
     RECEIVE, `TO` on SEND, `AT` in SEARCH's AT END, `PRINTING` after SUPPRESS — all legal COBOL the parser
     rejected. `scripts/spec/audit_grammar_optional_words.py` automates the search (11 grammar files, 529 rules).
   - **Three standing cautions, each earned:** a word measured un-underlined on ONE page is not enough — p634 and
     p732 CONTRADICT each other about `AFTER`/`SECONDS`, and the DEFINING clause wins (that check reverted a wrong
     change before it landed). `KEY`, `ON`, `RECORD` and `WITH` measure SPLIT and need per-site judgement. And the
     cheapest signal of all is **the grammar disagreeing with itself** about the same word.
   - Suggested order: §14 (489 items) first — GOBACK lives there and a too-restrictive statement rule bites
     hardest.
4. **FIGURE RENDERING — style SETTLED, generator WORKS, band detection DOES NOT.**
   `spec-reconciliation/FIGURE-STYLE.md` fixes how a general format is drawn: `<pre>` not a fence (a fence cannot
   carry underlining) · BOX DRAWING only (no Windows monospace font contains U+23A1–U+23AD, so those glyphs
   force per-glyph fallback and columns drift) · square brackets, curved braces with their point · `│` bars kept
   one space clear · **minimum three rows per group** · **`line-height: 1`**, without which the strokes do not
   tile. Every rule was settled by RENDERING candidates; four of six were found by the owner spotting a defect
   invisible in the markup.
   - `render_figure.py` generates a figure entirely from measurement — ACCEPT Format 3 comes out at its true
     SEVEN rows, which every hand-built version got wrong. It asserts that stripping the `<u>` tags reproduces
     the laid-out text, which is what makes a whole-standard sweep safe.
   - **BAND DETECTION IS FIXED and no longer needs `--band`.** `find_bands` reads the CLAUSE STRUCTURE — the
     bold `14.9.N.2 General format` heading opens the region, the next heading closes it, `Format N (…)` labels
     split it. Geometry cannot do this (row spacing *within* one format varies more than *between* formats).
     Whole standard: **475 figures on 339 pages, zero layout collisions.**
   - **A collision invariant now makes a bad layout LOUD.** A delimiter that would land on a character aborts
     the run: the corrupt form (`|N]-START` for `[ END-START ]`) still looks like a figure, so silence was the
     real risk. It immediately found six further defects, every one caused by INFERRING what the page states —
     bracket hand from a midpoint rather than the feet · a brace's point at the span's centre rather than its
     measured middle piece · delimiters merged by column rather than cut at their top hooks · a paren family
     (`æçè`/`ö÷ø`, COBOL's own full-height `(` `)`) drawn as literal letters.
   - **Both defects the first sheet exposed are FIXED**, each by measuring what was being inferred: a
     choice-indicator bar was adopting the foot of the bracket beside it (folio 503's FLOAT-DECIMAL rows — a
     foot must be ANCHORED at its own stem and extend away; 4 stems standard-wide, and the only other figure
     affected is the reference-format ruler, which carries no general format); and rule 5's spacer row ignored
     glyph-drawn delimiters, so every two-alternative BRACE came out two rows tall with no point (folio 276).
     Cross-checked against the printed page: folios 275/276 and 127 now render exactly as printed.
   - **SPACING settled from the printed page** (owner review of the first sheet): a BLANK ROW separates groups
     — located from the enclosures, since the gap cannot be measured as a gap (19.0 pt *between* the exception
     group and `[ END-ACCEPT ]`, 17.3 pt *inside* it) — and layout is now **per group, per cell**: columns
     align only where one delimiter spans both rows, and a cell (a run of words between delimiters) flows with
     single spaces. That removed every stray-gap artifact at its root, including `[ END-ACCEPT  ]`, which had
     been recorded as un-fixable by tolerance and was really a symptom of packing one column space figure-wide.
   - **The ASSIGN stretch is FIXED, and it was never the row-model question I reported it as.** An outer
     enclosure's label may not subdivide an inner one: `ASSIGN` sits on the OUTER brace's point row, which
     falls between the INNER brace's two alternatives, so `{ device-name-1 / literal-1 }` drew four rows where
     the identically-shaped `LOCK MODE IS { MANUAL / AUTOMATIC }` — with nothing enclosing it — drew three.
     Such a label now snaps to the nearer neighbouring row. A brace's OWN label is exempt, which is what keeps
     ACCEPT's `LINE NUMBER` (and its printed seven rows) correct.
   - **THE SWEEP IS BUILT AND KEYED ON THE CLAUSE HIERARCHY** — `scripts/spec/sweep_figures.py`
     (`--report` · `--apply` · `--check`). ⛔ **Never key on the page** (owner directive 2026-07-27: page
     numbering is to be REMOVED from the Markdown; references become intra-document links). A figure's identity
     is its clause + Format number, which is what `render_figure.figure_key` returns.
     - **The gate that makes it safe: WORDS unchanged, notation replaced.** A figure is rewritten only where
       the generated form carries the same words as the text it replaces; anything else is reported, never
       written. `--check` is the post-sweep regression gate and supersedes `audit_figure_structure.py`
       (which now reads `<pre>` as well as fences, so a swept document cannot report a false clean).
     - **THE GROUPING IS SOLVED, NOT GUESSED.** One printed figure is sometimes set as several markdown blocks
       (UNSTRING = two fences + a bare `[ END-UNSTRING ]`), and consecutive figures are sometimes separated by
       nothing but a figure note — no fixed merge rule separates those, and crossing notes fixed UNSTRING while
       breaking the file-control entry. `regroup` instead finds the partition of spans whose words match, and
       reports rather than picks when more than one partition does.
     - **✅ COMPLETE — 483 of 484 figures are generated from the printed page.** The one exception is
       deliberate and counted: §12.3.6.2 carries Addendum correction C3, and a figure whose region references
       `see the Addendum (Cn)` is never regenerated. `--check` reports zero mismatches, zero word differences,
       zero count disagreements; `verify_publishable.py` green.
     - The sweep REASSEMBLES fragmented figures (the file-control entry had Format 3 set as SEVEN blocks) and
       carries every figure note through after the figure it describes.
     - **EIGHT transcription defects found and corrected**, all of one family — duplication and run-in: two
       RUN-IN FORMAT LABELS destroying a Format boundary (§14.9.39.2 Format 10, §8.4.3.1.2 Format 3), the
       PICTURE Format 1 figure duplicated with CONTRADICTORY notes (they disagreed on whether `FOR` is
       underlined — grammar), a duplicated `DATA DIVISION.`, duplicated SPECIAL-NAMES feature/device lines, a
       duplicated method-definition figure, two figures written as PROSE BULLET LISTS (§12.3.6.2), and two
       ASCII-art figures.
     - **Four GENERATOR defects** found in the same pass, all under-reporting: a region spanning a heading-less
       page; a figure split because `[ sentence ] …` read as prose (notation is the signal); the same blind
       spot in the sweep's own test; and a wrapped prose-table tail read as a one-line figure (its tell is the
       unbalanced delimiter).
   - ⚠ **A whole-artifact invariant beats sampling.** Two apply-time duplication bugs were caught ONLY by the
     word-conservation check (151 then 97 words gained), both from `^\s*>` matching `>>` — a compiler
     directive is not a blockquote, and a blockquoted FIGURE is not a note.
   - ⚠ `audit_figure_structure.py` still reads FENCED blocks; it needs the `<pre>` form before the sweep lands.

5b. **⛔ THE ROAD TO ZERO GAP — THE EXECUTION PLAN (written 2026-08-03, measured not estimated).**
   v1.0 is defined as this inventory at zero GAP (D13). Everything below is computed from
   `tests/version-matrix/traceability-inventory.json`; **re-measure before trusting any number here.**

   **WHERE THE WORK ACTUALLY IS — and it is not where the review has been looking.**
   | | rows | adjudicated |
   |---|---|---|
   | §14 Procedure Division (statements) | **1141** | **0 %** |
   | §13 Data Division (clauses) | **983** | **0 %** |
   | §15 intrinsics | 546 | **60 %** ← *all four batches so far* |
   | §12 Environment Division | 293 | 0 % |
   | §8 concepts / reference format | 282 | 1 % |
   | §7 compiler directives | 252 | 0 % |
   | **Annex A implementor-documentation** | **222** | **0 %** |
   | §11 OPTIONS / configuration | 114 | 0 % |
   | §10, §6, §16, §5 | 28 | 0 % |

   By RULE KIND: **GR 1513 (0 %) · SR 1352 (0.2 %) · FMT 322 (19 %) · AR 226 (58 %) · RV 226 (59 %) · DOC 222 (0 %)**.
   ⛔ **AR and RV are essentially §15-ONLY kinds, and they are the two the review has driven.** GR + SR are
   **2,865 rows — 74 % of everything — and effectively untouched.** 327 of the 330 adjudicated rows are §15.

   ~~**⚠ THE CURRENT BATCH SHAPE DOES NOT GENERALISE…** `phase_b_batch.py` fans out ONE AGENT PER FUNCTION…
   it needs a per-STATEMENT subject key (and per-CLAUSE for §13).~~
   ⛔ **MEASURED 2026-08-03 — THIS PREMISE WAS WRONG TOO, AND THE REAL GATE IS SIZE.** `phase_b_batch.py` never
   had a per-FUNCTION key; it groups by the catalog's `subject`, and that field **is already the statement name
   in §14** ("SET statement", "READ statement" — 54 subjects over 1,141 rules) **and the clause name in §13**
   ("PICTURE clause", "VALUE clause" — 78 over 983). The key generalised all along.
   **What does not generalise is the batch SIZE.** The four §15 batches that have actually run — fanned out,
   adversarially refuted, landed — topped out at **18 rules per agent**. §14 has **18 subjects over 25 rules and
   5 over 40**, SET carrying **98**; §13 has 9 over 25 with PICTURE at 72; §12's SPECIAL-NAMES is 81. Handing one
   agent 98 rules is a five-fold departure from the only shape with evidence behind it.
   ✅ **DONE:** `phase_b_batch.py --max-rules` (default 20) splits an oversized subject **at CLAUSE boundaries
   first** — every construct is laid out `.2` general format / `.3` syntax rules / `.4` general rules, which are
   genuinely different questions, so the seam is free and it separates the FMT+SR vein from the GR vein without
   anyone arranging it. A subject that FITS is not split at all, preserving §15's proven whole-function shape
   (verified: a §15 batch still produces 12 subjects → 12 files, unchanged). A split part carries `part i of n`,
   its sibling slugs, and the WHOLE subject's clause map, so an agent knows what it is not seeing and is told to
   say so rather than guess. A partition invariant asserts no rule is dropped or duplicated — a lost rule would
   never be issued to any agent and nothing downstream would say so, the §11 A12c failure one register over.
   **THE PLANNING NUMBERS THIS UNLOCKS:** §14 = 90 agent files · §13 = 99 · §12 = 29 · §8 = 36 · §7 = 27 ·
   §11 = 16 · §15 (remaining) = 35.

   **⭐ THE SINGLE LARGEST EFFICIENCY WIN — TWO LEDGERS ARE COUNTING ONE BODY OF WORK.**
   Item 3 above (the grammar↔spec audit) scopes itself as **"1,659 items: 321 general formats + 1,338 syntax
   rules"**. The inventory's **FMT (322) + SR (1,352) = 1,674 rows**. Those are THE SAME TERRITORY, counted
   independently, differing by ~15 (inside the known denominator corrections). Run as two efforts it is ~1,670
   rules audited TWICE; run as one it is a single pass that emits an inventory verdict AND a grammar finding.
   **Do this before the SR mass is started, not after.** The `spec-grammar-conformance` skill already exists and
   already keys on clause numbers — it needs to emit `record_verdicts.py` batch files as well as its report.

   **THE FOUR VEINS, ordered by yield per unit of effort:**
   1. **DOC — 222 rows, closes by DOCUMENTING, not by code.** §4.2.16 requires the implementor-documentation
      items and D13 makes them part of v1.0.
      ⛔ **THIS VEIN'S PREMISE WAS WRONG, AND IT IS NOW MEASURED (2026-08-03).** It read: "`docs/CONFORMANCE.md`
      already carries a large numbered set, so an unknown but likely LARGE fraction of these are *already
      satisfied and merely unrecorded* … the cheapest closure in the whole inventory." **The measurement is 10
      of 199 obligations discharged; 189 remain** (`python scripts/spec/audit_annex_a1.py`). The "large numbered
      set" in CONFORMANCE.md is §2 (Annex **A.3**, processor-dependent) and §3 (Annex **E** behaviour
      determinations) — **different registers**. §7 is the A.1 register and it holds ten rows.
      ⚠ A clause-overlap search flatters badly and must not be used as a coverage proxy: 61 of 222 items have
      their cross-referenced clause cited *somewhere* in CONFORMANCE.md, but A.1-1…A.1-5 all cross-reference
      §14.9.1 and only ONE of them (the device, item 2) is actually determined.
      **So this is NOT the cheap vein.** Each of the 189 needs a determination to be *made* — read the
      implementation, settle the behaviour, write it, and pin it so the doc cannot drift from the code — which
      is 189 small design decisions, not a recording exercise. It still needs no compiler change *if* the
      behaviour is already settled, and that is the only sense in which it is cheaper than GR. ⚠ The original
      caution stands and earned itself immediately: **verify each against the clause rather than assuming
      CONFORMANCE.md's numbering lines up with Annex A.1's** — one entry was filed under the wrong item.
   2. **FMT + SR — 1,674 rows, unified with item 3** (above). Mostly mechanical: does the grammar admit exactly
      what the general format and syntax rules say? The vein already has a proven root cause (SR2's
      underlining-vs-bracketing) and a tool (`audit_grammar_optional_words.py`).
   3. **GR — 1,513 rows, the semantic mass.** The expensive vein: each is "does the implementation DO what the
      rule says". This is where the per-statement batch shape and the refute stage matter most.
   4. **THE FIX QUEUE — 268 open non-conforming rows today**, and it grows with adjudication.

   **THE RATE, AND WHY ADJUDICATION ALONE NEVER REACHES ZERO.**
   Of the 330 rows adjudicated so far, only **62 closed (19 %)**; **268 (81 %) OPENED work.** Batch 4 adjudicated
   77 rules and closed **zero**. If that ratio holds, adjudicating the remaining 3,531 rows yields roughly **660
   closures and ~2,870 open findings**. **Adjudication maps the territory; only Phase C closes it.** Plan for
   both, interleaved — a pure-adjudication run drives the GAP DOWN not at all and the queue UP steeply.
   ⚠ **CLUSTER BEFORE COUNTING THAT AS 2,870 DEFECTS.** Batch 4's 77 findings were 9 queue items: 33 were ONE
   owner-ratified disposition and 20 were ONE root cause. The observed finding:defect ratio is ~8:1, and every
   prior batch reported the same shape.

   **THE CLOSING BOTTLENECK IS TESTS, NOT VERDICTS.** A CONFORMS row stays a GAP until a **spec-derived** test
   covers it (`nist:` and `characterization:` never qualify — §8's `spec-derived` flag enforces it). Today all 62
   CONFORMS rows happen to be tested, so the bottleneck is invisible; **it appears the moment a batch produces
   CONFORMS rows in bulk.** Budget golden-writing as a first-class phase, and prefer ONE golden covering many
   rules of a clause over one per rule.

   **EXECUTION ORDER (each step's output makes the next cheaper):**
   | # | step | why here |
   |---|---|---|
   | 1 | ✅ **MOSTLY DONE 2026-08-03 — §11 A12 · A12b · A12c · A12e CLOSED. A12d's STRUCTURAL half is done; its DISTRIBUTION is still uncollected, and A13 (b) is unmeasured — neither blocks step 2.** | Until they closed, "all legs green" was not evidence, and EVERY step below is validated by that battery. The rule is `DESIGN-test-build-ci.md` §3.10 (the verdict-evidence invariant): a verdict needs the observation it claims, a run's results must PARTITION its declared population, and the expectation comes from a committed manifest — never a remembered number. ⚠ It also closed a defect this list did not anticipate: the same process-runner bug existed SEVEN times across five test projects. |
   | 2 | ⚠ **AUDITED 2026-08-03 — and the answer inverts this row.** The audit itself is DONE and is now a gate (`scripts/spec/audit_annex_a1.py --check`, self-tested, proven to fail on a real defect). | This row said "highest closure per unit effort … the one vein that can move the GAP number visibly early". **Measured: 10 of 199 obligations discharged, 189 remain**, and the "already documented" mass it assumed was Annex A.3 / Annex E, which are different registers. The 189 are determinations to be MADE, not recorded. It moves the GAP number no faster than any other vein — but it is still the only one needing no compiler change, and the audit has made the remaining set exact and drift-proof, which is what step 2 was really worth. **Re-rank the veins against this before planning the next campaign phase.** |
   | 3 | ✅ **DONE 2026-08-03 — but not the change this row named.** | The subject key was ALREADY per-statement/per-clause; the real gate was per-agent SIZE (§15's proven max is 18 rules; SET is 98). `phase_b_batch.py --max-rules` now splits an oversized subject at clause boundaries, leaves a fitting subject whole, and asserts the partition loses nothing. Gates the 2,124 rows in §14+§13 as this row intended. Planning numbers: §14 = 90 agent files, §13 = 99. |
   | 4 | ✅ **DONE 2026-08-03 — one pass now emits both a grammar finding and an inventory verdict batch.** `.claude/workflows/spec-grammar-conformance.js` findings carry `rule_ids`; `scripts/spec/grammar_findings_to_batch.py` converts them to a `record_verdicts.py` batch (MATCHES→CONFORMS · DIVERGES→DIVERGES · NOT-IMPLEMENTED→NOT-IMPLEMENTED · **UNCLEAR→nothing**), and `--coverage` names every FMT/SR rule the pass did NOT settle. ⛔ **It REFUSES to expand a finding across a section's other rules** — stamping one observation onto §14.9.1's 19 rules would manufacture coverage, so an unnamed rule stays a GAP. Proven end-to-end: synthetic findings → batch → `record_verdicts.py --dry-run` accepted 3 records (2 CONFORMS-but-untested, 1 DIVERGES) and correctly closed **nothing** — a grammar MATCHES records a verdict but still needs a spec-derived test to close the row. `--self-test` covers 9 cases. ⚠ Its own first draft counted an UNCLEAR rule as covered, so a pass deciding nothing would have reported FULL coverage; fixed and pinned. | Avoids auditing ~1,670 rules twice. Do it BEFORE starting the SR mass. ⭐ **The premise CHECKS OUT** — unlike steps 1–3, this one survived measurement: the grammar audit's "1,659 items = 321 general formats + 1,338 syntax rules" against the catalog's FMT 322 + SR 1352 = 1,674, and the deltas of 1 and 14 are exactly §0's recorded denominator corrections. Same territory, counted twice. ⛔ **But the audit could not RUN.** It is `.claude/workflows/spec-grammar-conformance.js` (a WORKFLOW, not a "skill" as this list said), and de-paging broke it two ways: it read `r['page']` from every catalog row — **0 of 3,861 rows carry one** — and it matched sections EXACTLY, so its own documented example args (`14.9.1,14.9.2`) selected **zero rules and printed nothing**, silently handing each agent no page and no rules. Fixed: `scripts/spec/clause_page.py` resolves a clause to its printed folio + PDF page (and renders it), **exiting non-zero when a clause does not resolve**, and the workflow now prefix-matches. Verified: `14.9.1` → 19 rules (was 0) and folio 576. Swept for siblings — no other consumer of the removed catalog `page` field exists. |
   | 5 | **Drain the fix queue to zero once** (PB12, PB14–PB18, PB23–PB27 + residue) — ⚠ **STARTED 2026-08-03: PB18 RE-VERIFIED, deliberately NOT implemented.** | A queue carried across a 40-batch campaign becomes unreviewable; empty it while it is 12 items. ⭐ **PB18's every claim HOLDS** — both citations validate, the repro is exact (`10 ** 30` → `1000000000000000071935427891953`), the named emitter site is real, and `CobolDec.Pow`'s square-and-multiply really is the shape to copy. ⛔ **But its recipe does not answer the question the fix turns on: SCALE EXPLOSION.** Exact `Int128` repeated multiplication is easy only for a scale-0 base; a scale-*s* base to the *n* yields scale *s·n*, so `1.5 ** 30` needs ~36 significant digits before the receiver's capacity is even considered. The fix must decide what happens when the exact result does not fit — EC-SIZE-EXPONENTIATION (`CobolDec.Pow`'s precedent) or the documented double fallback for that case alone. §8.8.1.3 licenses either; D3's "exact Int128 fixed-point engine" makes it a DOCUMENTATION question. **That is a numeric-design decision for `COBOLNET_NUMERIC_DESIGN.md`, not a code edit** — start there, not in the emitter. |
   | 6 | **§14 then §13 GR/SR batches, interleaved with fixing** | The semantic mass, in the two clauses that dominate it. |
   | 7 | **§12, §7, §8, §11, then the small clauses** | The tail. |

   ⚠ **DO NOT DROP THE REFUTE STAGE TO GO FASTER.** It is the single most productive part of the loop: every
   overturn across four batches was a DOWNGRADE, it found PB5 and PB7, and in batch 4 it found a regression
   introduced hours earlier in the same session. An all-CONFORMS report is a red flag, not a good result.

5. **PHASE-14 STEP-0 — the FULL implementation↔spec review**, plan
   `docs/rearchitecture/DESIGN-spec-conformance-review.md`. **Phase A is DONE:** `spec-rule-catalog.json` holds the
   denominator — **3,861 items** (1352 SR · 1513 GR · 226 AR · 226 RV ·
   222 Annex-A.1 doc obligations · 322 general formats). ⚠ It was **3,790 until 2026-07-30**, when
   the extractor's literal heading-spelling map was found to have silently skipped whole clauses; regenerate with
   `python scripts/spec/extract_rule_catalog.py`, which now also REPORTS any rule-shaped heading it cannot
   resolve. The traceability
   inventory exists at `tests/version-matrix/traceability-inventory.json` and session-probe now reports the live
   GAP. Phase B = map + verify each rule → code → verdict (resumable; verdicts persist across sessions) · Phase C =
   close every DIVERGES / NOT-IMPLEMENTED / untested-CONFORMS. **The inventory at zero GAP = P14 DONE = D13.**

**✅ THE COMPREHENSIVE PRE-MERGE GATE RAN AND `phase-14` WAS MERGED (2026-07-28, `c056f1f4`).** Every leg green,
⚠ **HISTORICAL — that merge is the 2026-07-28 one. A NEW WAVE (V59 + DA1–DA7) has since landed on `phase-14`
and is 18 commits AHEAD of `main` = `0e534dc7`, UNMERGED. See §0 "Where we are" for the current state; the
battery numbers below are superseded by §0's battery reference.**
zero regressions. The table below is the EARLIER run of that gate, kept because it records the EC-infra + OO
super-batch (**10 findings landed + CA12 REFUTED**) and the one red it found — the VCR's dangling spec LINE
citations, since closed by re-keying them onto the clause hierarchy. **The MERGE gate's numbers are:**

| leg | result |
|---|---|
| greenfield Conformance | **4113 / 4113** — nothing red |
| greenfield Unit | **580 / 580** |
| characterization | **33 / 33** byte-identical |
| `guard-fast.sh` | **=== ALL GREEN ===** (exit 0) — NIST **353 MATCH / 0 REGRESSION** |
| legacy Unit / Integration | 1203/1203 · 503/504 (1 skipped) |
| GnuCOBOL differential | **0 REGRESSIONS.** Per-case diff: 3 flips, ALL pre-existing CA34 (`COBOLNET1625`) — verified, not inherited, via `git merge-base --is-ancestor f54c9bd4` against the branch tail. |

| leg | result |
|---|---|
| characterization | **33/33** byte-identical |
| `guard-fast.sh` (legacy + NIST) | **ALL GREEN — NIST 353 MATCH / 0 REGRESSION**, matching the recorded baseline |
| legacy Unit / Integration | 1203/1203 · 503/504 (1 skipped) |
| greenfield Unit | **580/580** — after fixing 2 `CobolPtrTests` that encoded CA9's pre-decision throw |
| greenfield Conformance | **3929 passed · 1 failed · 3930 total** (12m 05s), reproduced exactly on a clean re-run. The single failure was `VcrDriftTests.EverySpecLineRef_IsWithinTheSpec` — pre-existing, and now CLOSED (that test is replaced by `EverySpecCitation_ResolvesInTheSpec` + `NoSpecLineNumberIsCited_InTheVcr`). Nothing else failed. |
| GnuCOBOL differential | **0 regressions from this batch.** Per-case diff vs the stored report: 3 flips — 1 fix, and 2 AGREE_ACCEPT→WE_REJECT_THEY_ACCEPT in `syn_value.at` ('Numeric item with picture P', 'Numeric item (non-integer)'), BOTH attributable to **CA34** (`COBOLNET1625`, introduced by `f54c9bd4`, present at this session's start commit), not to the EC batch. |

✅ **THE BASELINE IS REFRESHED at the phase-14 merge (2026-07-28) — and note WHICH baseline is durable.**
⛔ `tests/external/gnucobol-differential-report.json` is **GITIGNORED** (`.gitignore:85`), so it is a per-machine
LOCAL artifact, not a committed "before": a fresh clone has none and the first run there can only establish one,
never diff against one. **The committed baseline is the numbers in this section.** Both now read
**472 AGREE_ACCEPT · 173 AGREE_REJECT · 574 WE_REJECT_THEY_ACCEPT · 104 WE_ACCEPT_THEY_REJECT over 1323 cases**,
so a per-case diff on this machine has a truthful "before" and the numbers survive a clone.
(Committing the report would make per-case diffs portable and is GPL-clean — §0's own rule allows their titles
and keywords, which is all it carries of theirs. Not done here: that is a .gitignore policy change, not a merge.)
The three flips the refresh absorbs are all CA34
(`COBOLNET1625`, `f54c9bd4` — verified an ancestor of the merge's start commit, so none is from that work): two
AGREE_ACCEPT→WE_REJECT_THEY_ACCEPT in `syn_value.at` and one WE_ACCEPT_THEY_REJECT→AGREE_REJECT. They are a
deliberate spec-derived tightening (§13.18.63.3 SR2/SR3) that GnuCOBOL's DEFAULT_DIALECT accepts as an
extension — not a bug to chase.

⚠ **Sequencing lesson from this run, worth repeating:** do NOT start `guard-fast.sh` while a `--no-build`
Conformance run is in flight — guard rebuilds, and the first Conformance run produced no verdict at all as a
result. Run the long legs ONE AT A TIME.

### Gates — what to run, and when

- **PER COMMIT — the WAVE-LOCAL filtered gate ONLY (~2 min):** the fix's own tests + immediate neighbours +
  characterization + the relevant unit filter. Example: `dotnet test tests/Cobol.Net.Tests.Conformance --filter
  "FullyQualifiedName~<Area>"`; for an edition gate ALSO `--filter "FullyQualifiedName~VersionMatrix"`.
  ⛔ Do NOT run the full ~11–20 min Conformance suite per fix (owner-corrected; §3 and
  `feedback_tiered_gates`).
  ⛔ **PICK THE FILTER FROM WHAT THE CHANGE TOUCHES, NOT FROM WHERE THE NEW GOLDENS LIVE.** A PB36 wave filtered
  on `~Corpus` (440/440 green) because that is where its goldens sat; the change was to MODULE-NAME semantics,
  whose tests are named for it, and a green test pinning the OLD behaviour sailed through to the comprehensive
  gate. `~Corpus|~ModuleName` gives 447/447 in the same ~20 s. Filters are cheap and OR-able — name the SUBJECT
  (`~Arithmetic|~Inspect|~Intrinsic`), not just the corpus.
- **PER ACCUMULATED BATCH / PRE-MERGE — the comprehensive gate:** full greenfield Conformance + characterization +
  the GnuCOBOL differential, plus `scripts/guard-fast.sh` (~3.3 min parallel) when a legacy-shared seam is touched
  — never the ~20 min serial `guard.sh`.
- **The legacy differential is OPT-IN** (`COBOLSHARP_LEGACY_DIFFERENTIAL=1`) and NO new `GreenfieldOnly` exclusions
  are added — greenfield registration alone suffices. The legacy engine + `guard.sh` survive ONLY for the P14
  Step-0 equivalence proof; deletion is P15.
- **GnuCOBOL external differential — USE IT (owner directive):** `python3 scripts/gnucobol_differential.py --exe
  src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.exe --report <path>` over the fetched GPL corpus (1323 groups,
  git-ignored `tests/external/gnucobol/`; run `fetch-gnucobol-tests.ps1` if absent). It catches reference-format
  and grammar bugs the internal battery is blind to.
  ⛔ **IT IS NOW A GREEN/RED GATE, AND THE DIFF IS MECHANICAL — do NOT hand-compare runs any more.** It diffs
  each case against the committed `tests/external/gnucobol-verdict-baseline.tsv` and prints
  `=== DIFFERENTIAL: n PER-CASE FLIP(S) ===`; **0 is required**, an absent baseline is a RED, and NEW/REMOVED
  cases are reported as a corpus refresh rather than counted as flips. A FIX is a divergence→AGREE flip, a
  REGRESSION is an AGREE→divergence flip; both show up as named rows, and both require the baseline be
  regenerated with `--write-baseline` in a commit that ATTRIBUTES every changed row. ⚠ Never read the four
  summary totals as the verdict — offsetting flips leave them identical, which is exactly what this replaced.
  ⚖ GPL: never commit or reproduce their source or expected output — titles and
  keywords only.
- **Mechanics that have burned us:** build `CobolSharp.sln` (not one project) before ANY `--no-build` test or CLI
  smoke — a stale test-bin compiler DLL hides regressions. Never `| tail -N` a guard verdict: redirect the FULL
  output to a file, then `grep 'ALL GREEN'` + `grep -iE 'crash|abort|Failed: *[1-9]'`. A high-JOBS parallel NIST
  leg can FALSE-RED — re-run the NAMED test serially before believing a regression, and never `taskkill
  dotnet.exe` immediately before a guard. Gating a construct's `introducedIn` breaks every test/golden that
  compiles it below the new edition — sweep and re-bake in the same change set.
  ⛔ **DO NOT EDIT COMPILER SOURCES WHILE `battery.sh` IS RUNNING — PHASE 2 REBUILDS.** Phase 1 is `--no-build`,
  which makes editing look safe; `guard-fast` in phase 2 is not, so a mid-run edit silently splits the battery
  across two trees and every leg after phase 1 becomes unattributable. This cost a full 11-minute run on
  2026-08-04. The `fleet_active_build.py` guard covers subagent FLEETS, not batteries — this one is on the human
  side of the loop. If sources must change, stop the run, then re-run it whole.
- ✅ **THAT OWED GATE HAS BEEN PAID — the comprehensive battery ran on the PB17+PB41+PB42 tree and printed
  `=== BATTERY: ALL GREEN ===`** (2026-08-04 19:13, one `bash scripts/battery.sh` run, artifacts
  `/tmp/battery-20260804-191318`). It therefore covers PB15 as well, which had only ever had the wave-local gate.
  See the BATTERY REFERENCE below for the numbers — they are not restated here (single-write rule).
- ✅ **THE DIFFERENTIAL NOW PROVES ZERO PER-CASE FLIPS MECHANICALLY — it had been asserting them from four
  totals.** Identical totals are consistent with OFFSETTING flips, so "0 per-case flips" was an inference, and no
  per-case report from a prior run had even been kept to diff against.
  **`tests/external/gnucobol-verdict-baseline.tsv` is now COMMITTED** — 1323 rows of `id⇥tier⇥verdict`, every
  column ours (owner decision 2026-08-04: names+verdicts are our generated results; their SOURCE and EXPECTED
  OUTPUT remain uncommitted, and titles/keywords are deliberately omitted since the diff does not need them).
  `gnucobol_differential.py --baseline` diffs against it, names each flip with that case's first diagnostic,
  counts NEW/REMOVED cases as a corpus refresh rather than a flip, and **treats an ABSENT baseline as a RED**.
  `battery.sh` gates on the `=== DIFFERENTIAL: n PER-CASE FLIP(S) ===` verdict line, never the exit code.
  ⛔ **Regenerating it (`--write-baseline`) is deliberate, and every changed row must be attributed in the commit
  that does it** — rewriting a baseline to clear a red destroys the only record that behaviour moved.
  ⛔ **The detector was proven in the failing direction before it was trusted**, including the exact blind spot:
  two offsetting flips that leave all four totals at 559/487/176/101 are still both named. A fresh full run then
  returned **0 flips** against the committed baseline.
- **⛔ BATTERY REFERENCE — CURRENT, the six-landing backlog tree `e103a6e3` (2026-08-09 13:49).**
  ✅ **`=== BATTERY: ALL GREEN ===` as measured, one `bash scripts/battery.sh` run:** FULL greenfield
  Conformance **4313 / 4313, zero skipped** (12 m 58 s) · greenfield Unit **4122 / 4122** ·
  characterization **33 / 33** · `guard-fast` **ALL GREEN** with NIST **353 MATCH / 0 REGRESSION** ·
  GnuCOBOL differential **`0 PER-CASE FLIP(S)`** against the committed baseline. This is the comprehensive
  gate the 2026-08-09 six-landing batch (DEVLOG 1257–1262) was owed — each landing had run wave-local only.
- **⛔ PRIOR BATTERY REFERENCE — the R33+R38 closing tree `b66a292c` (2026-08-08 20:05).**
  ⚠ **NOT GREEN as measured — all THREE differential flips pre-understood, two of them AGREEMENT:**
  FULL greenfield Conformance **4303 / 4303, zero skipped** (12 m 58 s) · greenfield Unit **4099 / 4099** ·
  characterization **33 / 33** · `guard-fast` **ALL GREEN** with NIST **353 MATCH / 0 REGRESSION** and
  **AUDIT CLEAN** · differential: `run_misc:1759` → WE_REJECT carrying R38's Format-4 message — the
  ADJUDICATED divergence, attribution pre-recorded in the note before the battery ran; `syn_definition:852`
  and `:878` → **AGREE_REJECT** — GnuCOBOL's own ambiguity testcases now rejected by R33's §8.4.2.2.1
  enforcement, moving INTO agreement, with ZERO over-rejections anywhere in the 1,323-case GPL population
  (R33's remaining arbitration, closed). Baseline regenerated, EXACTLY 3 rows; fresh differential
  **`0 PER-CASE FLIP(S)`**. **This battery closed the R-wave: `work.py next` reports the ranked actionable
  list EMPTY.**
- **⛔ PRIOR BATTERY REFERENCE — the R23+R36+R39+R28 tree `0cfda3fa` (2026-08-08 19:20).**
  ⚠ **NOT GREEN as measured — BOTH reds attributed, one of them a real find:** guard-fast **ALL GREEN**
  with NIST **353 MATCH / 0 REGRESSION** and **AUDIT CLEAN** · greenfield Unit **4099 / 4099** ·
  characterization **33 / 33** · Conformance **4293 / 4294** — the ONE red was the version-matrix row
  `date-to-yyyymmdd-2002`, whose program bundles SECONDS-PAST-MIDNIGHT into the "2002 quartet": R28's
  re-windowing (SPM → 2014 per the WG4 CD's D.2 list) correctly broke the row's claim, and the follow-up
  commit SPLITS it (the trio stays 2002; the new `seconds-past-midnight-2014` row expects 1502 below
  2014; full VersionMatrix re-run **2026 / 2026** + registry drift green) · the differential reported
  ONE flip, `listings:205` `AGREE_ACCEPT → WE_REJECT_THEY_ACCEPT` carrying R39's **COBOLNET1641** — the
  GCOS/ACU literal-REPLACE form (`REPLACE LEADING "X" BY SPACES …`), verified against the case source:
  the vendor construct R36 adjudicated, never ISO-legal, now honestly rejected. Baseline regenerated,
  EXACTLY 1 row; fresh differential **`0 PER-CASE FLIP(S)`** (1323 cases).
- **⛔ PRIOR BATTERY REFERENCE — the R34+R27+R29+R35 tree `7590ce35` (2026-08-08 17:53).**
  ⚠ **NOT GREEN as measured, and the two flips were R35's PREDICTED RECOVERY:** every internal leg green —
  FULL greenfield Conformance **4291 / 4291, zero skipped** (11 m 41 s) · greenfield Unit **4099 / 4099** ·
  characterization **33 / 33** · `guard-fast` **ALL GREEN** with NIST **353 MATCH / 0 REGRESSION** and
  **NIST AUDIT CLEAN** — and the differential reported exactly TWO flips, `listings:2598` and
  `run_functions:4457`, both **WE_REJECT_THEY_ACCEPT → AGREE_ACCEPT**: the bare zero-argument UDF cases
  R35 fixed now compile and AGREE with the oracle (its LANDED section named both in advance). R27's index
  rejections and R29's arithmetic rejection moved nothing — no DEFAULT-dialect corpus case exercises
  either shape. Baseline regenerated, the TSV diff EXACTLY 2 rows; a fresh differential prints
  **`=== DIFFERENTIAL: 0 PER-CASE FLIP(S) ===`** (1323 cases).
- **⛔ PRIOR BATTERY REFERENCE — the R31+R32+R25+R26 tree `2a0898c7` (2026-08-08 16:48),
  `=== BATTERY: ALL GREEN ===`:** FULL greenfield Conformance **4285 / 4285, zero skipped** (10 m 49 s) ·
  greenfield Unit **4094 / 4094** · characterization **33 / 33** · `guard-fast` **ALL GREEN** with NIST
  **353 MATCH / 0 REGRESSION** and **NIST AUDIT CLEAN** · GnuCOBOL differential **1323 cases,
  `=== DIFFERENTIAL: 0 PER-CASE FLIP(S) ===`** against the baseline the R31+R32 commit regenerated with
  its 38 attributed rows. Covers the candidate-set qualified resolver (every qualified reference in every
  corpus/NIST program re-resolved through the rewrite and nothing moved), the screen/alphabet-name
  exemptions, R25's UTC-roll guard and R26's algebraic-fold arms.
- **⛔ PRIOR BATTERY REFERENCE — the R21+R22+R30 tree `36ce29fa` (2026-08-08 15:51).**
  ⚠ **THIS RUN'S VERDICT LINE SAID `NOT GREEN`, AND THAT WAS THE GATE DOING EXACTLY ITS JOB** — the
  differential was the one leg that could see what R30's new rejection does to REAL programs, and it did:
  every internal leg green — FULL greenfield Conformance **4279 / 4279, zero skipped** (12 m 17 s) ·
  greenfield Unit **4089 / 4089** · characterization **33 / 33** · `guard-fast` **ALL GREEN** with NIST
  **353 MATCH / 0 REGRESSION** and **NIST AUDIT CLEAN** — and the differential reported **41 per-case
  flips, every one carrying R30's COBOLNET1639 (or R22's COBOLNET1543), ALL ATTRIBUTED**: 14 moved TOWARD
  the oracle (AGREE_REJECT — GnuCOBOL-rejected programs we used to compile clean via silent staging,
  including four syn_definition ambiguity cases that independently validate the R31 rewrite); 3 were REAL
  over-rejections R30 exposed, fixed in the follow-up commit (R31 qualified matching · R32 screen names ·
  R38's alphabet-name diagnostic); 24 are spec-correct strict rejections of vendor constructs or open
  register notes (R34 recursive COPY REPLACING · R35 bare zero-arg UDF · R36 partial REPLACE · R37 the
  RETURN-CODE/LENGTH-OF register family · R38's construct half). Baseline regenerated in the follow-up
  commit — the TSV diff is EXACTLY 38 rows (41 − the 3 fixed), each owned by the list above; a fresh
  differential prints **`=== DIFFERENTIAL: 0 PER-CASE FLIP(S) ===`** (1323 cases).
- **⛔ PRIOR BATTERY REFERENCE — the R17+R19+R20 tree `bc739114` (2026-08-08 13:38),
  `=== BATTERY: ALL GREEN ===`:** FULL greenfield Conformance **4275 / 4275, zero skipped** (11 m 52 s) ·
  greenfield Unit **4086 / 4086** · characterization **33 / 33** · `guard-fast` **ALL GREEN** with NIST
  **353 MATCH / 0 REGRESSION** and **NIST AUDIT CLEAN** · GnuCOBOL differential **1323 cases,
  `=== DIFFERENTIAL: 0 PER-CASE FLIP(S) ===`** (artifacts `/tmp/battery-20260808-131723`). Covers R17
  (the signed float literal as ONE token in every lexer region — a GRAMMAR change, so the differential
  was the leg to watch, and it moved nothing), R19 (`COBOLNET1638`) and R20 (FIND-STRING's positional
  phrase walk). The docs-only handoff commit `e922c011` landed after the battery's build phase, so the
  measured code tree is `bc739114`'s.
- **⛔ PRIOR BATTERY REFERENCE — the R24+R15+R16+R12 tree `db84240b` (2026-08-08 12:27).**
  ⚠ **THIS RUN'S VERDICT LINE SAID `NOT GREEN`, AND THAT WAS THE GATE WORKING** — the PB40 shape again. Every
  internal leg was green: FULL greenfield Conformance **4271 / 4271, zero skipped** (11 m 7 s) · greenfield
  Unit **4083 / 4083** · characterization **33 / 33** · `guard-fast` **ALL GREEN** with NIST **353 MATCH /
  0 REGRESSION**. The one red was the differential naming EXACTLY ONE flip: `run_file:13011`,
  `AGREE_ACCEPT → WE_REJECT_THEY_ACCEPT`, carrying R16's own `COBOLNET1637` — the case DISPLAYs an
  index-name, GnuCOBOL's documented extension, which §13.18.38.3 r7's closed context list makes illegal;
  our pre-R16 "accept" was a compile-clean runtime abort. Baseline regenerated (`--write-baseline`), the TSV
  diff exactly one row, attributed in `c6909f89`; a fresh differential prints
  **`=== DIFFERENTIAL: 0 PER-CASE FLIP(S) ===`**. (COBOLNET1637 is deliberately unconditional — no
  `--permissive` coercion; the string band had nothing to coerce and R29 holds the broader adjudication.)
- **⛔ PRIOR BATTERY REFERENCE — the R10 unsigned-carrier tree `5312996a` (2026-08-08 00:26),
  `=== BATTERY: ALL GREEN ===`:** FULL greenfield Conformance **4263 / 4263, zero skipped** (11 m 24 s) ·
  greenfield Unit **4077 / 4077** · characterization **33 / 33** · `guard-fast` **ALL GREEN** with NIST
  **353 MATCH / 0 REGRESSION** · GnuCOBOL differential **`=== DIFFERENTIAL: 0 PER-CASE FLIP(S) ===`**
  (artifacts `/tmp/battery-20260808-001031`). Covers the whole R10 COMP-5 unsigned-carrier change set
  (ulong/UInt128 carriers, the WrapBinary bits=128 fix, the checked additive siblings) — notable because a
  numeric-substrate change is exactly the class the differential exists to catch, and it moved nothing.
- **⛔ PRIOR BATTERY REFERENCE — the PB40 tree (2026-08-06 03:25).**
  ⚠ **THIS RUN'S VERDICT LINE SAID `NOT GREEN`, AND THAT WAS THE GATE WORKING** — the same shape as the
  PB47 run. Its one red was the differential reporting **1 per-case flip**: `run_functions:221`,
  `AGREE_ACCEPT → WE_REJECT_THEY_ACCEPT`, carrying PB40's own `COBOLNET1627`. §0's mechanical rule classes
  an AGREE→divergence flip a REGRESSION; attributed from the CASE, it is the opposite. The test passes a
  `PIC S9(4)V9(4)` item to `FUNCTION CHAR`, and §15.15.3 r1 requires an integer while §15.3 type 6 admits
  only an integer data item or an always-integral expression — **illegal COBOL that GnuCOBOL accepts and we
  now reject**, with `--permissive` accepting it (verified) so migration is not blocked. Baseline
  regenerated with `--write-baseline`; the diff is EXACTLY ONE ROW and a fresh differential prints
  **`=== DIFFERENTIAL: 0 PER-CASE FLIP(S) ===`**. Every other leg was green on this same tree and the TSV is
  read by that one leg only, so nothing is composed across trees:
  FULL greenfield Conformance **4219 / 4219, zero skipped** (11 m 51 s) · greenfield Unit **3986 / 3986** ·
  characterization **33 / 33** · `guard-fast` **ALL GREEN** with NIST **353 MATCH / 0 REGRESSION** and
  **NIST AUDIT CLEAN**.
- **⛔ PRIOR BATTERY REFERENCE — the PB12+PB30+PB31 tree (2026-08-06 02:05),
  `=== BATTERY: ALL GREEN ===`:** FULL greenfield Conformance **4216 / 4216, zero skipped** (11 m 10 s) ·
  greenfield Unit **3986 / 3986** · characterization **33 / 33** · `guard-fast` **ALL GREEN** with NIST
  **353 MATCH / 0 REGRESSION** and **NIST AUDIT CLEAN** · GnuCOBOL differential **1323 cases,
  0 PER-CASE FLIPS**.
  ⭐ **THAT DIFFERENTIAL RESULT IS THE LOAD-BEARING ONE.** The wave added FIFTEEN new bind-time REJECTIONS
  to the §15.3 argument screen, and PB1's disaster was a screen that turned away 12 LEGAL corpus programs —
  so "it rejects the right things" is the CHEAP half of the gate. 1,323 real GPL COBOL programs changing no
  accept/reject verdict, plus 599 corpus programs and 353 NIST programs green, is the over-rejection
  evidence; a probe of hand-written illegal cases is only the under-rejection half.
- **⛔ PRIOR BATTERY REFERENCE — the PB48 tree (2026-08-06 00:55), `=== BATTERY: ALL
  GREEN ===`:** FULL greenfield Conformance **4212 / 4212, zero skipped** (10 m 29 s) · greenfield Unit
  **3979 / 3979, zero skipped** · characterization **33 / 33** · `guard-fast` **ALL GREEN** with NIST
  **353 MATCH / 0 REGRESSION** and **NIST AUDIT CLEAN** · GnuCOBOL differential **1323 cases**,
  **0 PER-CASE FLIPS** against the committed baseline.
  ⚠ Unit moved 3702 → 3979 because `ParenTokenTwinDriftTests` is a file-per-row theory. Conformance moved
  4209 → 4212 with PB48's goldens. **Zero failures and zero skipped is what must hold, never a total.**
  ⛔ **THE RUN BEFORE IT, ON THE SAME COMMIT'S CODE, EXITED 0 AND WAS NOT GREEN** — verdict
  `=== FAILURES: nist=31 audit=1 unit_rc=0 int_rc=1 baselines=0 ===`, NIST 353 → **322 MATCH / 31 REGRESSION**,
  every one the `IF` (intrinsic function) suite, every one a clean compile throwing at RUN TIME. PB48 split one
  lexeme into two token types and swept the GRAMMAR consumers but not the CODE ones; the legacy oracle's
  `MapFunctionArgTokens` had no arm for the new twin. ⭐ **The wave-local gate, FULL greenfield Conformance, the
  unit suite, characterization AND the GnuCOBOL differential were ALL GREEN on that broken tree** — the
  differential's documented blind spot exactly (compilability unaltered, runtime output changed). CI reproduced
  it on Linux as `killed by signal 6`, scored `RUN NO-VERDICT`. Guarded now by `ParenTokenTwinDriftTests`.
- **⛔ PRIOR BATTERY REFERENCE, for the differential-baseline provenance only — the PB23+PB25+PB47 tree (2026-08-05 22:30).**
  ⚠ **THIS RUN'S VERDICT LINE SAID `NOT GREEN`, AND THAT WAS THE GATE WORKING.** Its one red was the differential
  reporting **1 per-case flip** — `syn_misc:3506`, `WE_ACCEPT_THEY_REJECT → AGREE_REJECT`, carrying PB47's own
  `COBOLNET1634`. By §0's own rule a divergence→AGREE flip is a **FIX**: a program we had been WRONGLY ACCEPTING
  is now rejected, and GnuCOBOL had been rejecting it all along — independent corroboration of the Table 15
  reading, from the one net that could contradict it. The baseline was regenerated with `--write-baseline` in the
  landing commit with that single row attributed, and a fresh differential against it prints
  **`=== DIFFERENTIAL: 0 PER-CASE FLIP(S) ===`**. Every other leg was green on this same tree, and the TSV is read
  by that one leg only, so nothing is composed across trees:
  FULL greenfield Conformance **4209 / 4209, zero skipped, NOTHING red**
  (11 m 23 s) · greenfield Unit **3689 / 3689, zero skipped** · characterization **33 / 33** · `guard-fast`
  **ALL GREEN** with NIST **353 MATCH / 0 REGRESSION** and **NIST AUDIT CLEAN** · GnuCOBOL differential
  **1323 cases**, totals **559 WE_REJECT_THEY_ACCEPT · 487 AGREE_ACCEPT · 177 AGREE_REJECT ·
  100 WE_ACCEPT_THEY_REJECT** (176→177 / 101→100 is the one attributed case moving), and — measured against the
  regenerated per-case baseline, not inferred from those totals — **0 PER-CASE FLIPS**.
  ⚠ Conformance moved 4180 → 4209 because each landed fix ships its goldens. What must hold is **zero failures,
  zero skipped**, never a particular total.
  ⚙ **THIS RUN IS THE STRONGEST EVIDENCE THE GATE HAS PRODUCED FOR A GRAMMAR CHANGE.** It measured an ARITY
  change to `evaluateWhenGroup` — in reach of every EVALUATE statement in the corpus, in NIST and in the GnuCOBOL
  differential — and **nothing moved**: 0 NIST regressions, 0 per-case flips, no conformance red. That is what
  says the repetition it removed was dead for legal source (PB45), rather than merely that the new goldens pass.
  ⚙ **A BATTERY ON THE PB43 TREE (2026-08-04) CAME BACK RED, AND THE RED WAS THE GATE'S OWN.**
  `GrammarDiagramGeneratorDriftTests` hit a Windows file-handle race reading back `rr.war`'s output under the
  parallel load (**PB44** — fixed, not merely filed); it passed on a serial re-run of the SAME tree, and the
  generator touches ANTLR fragments rather than anything that wave changed. Kept here as standing guidance:
  **attribute every red by name — a gate that can go red without a defect trains everyone to discount reds.**
- ⚙ **THE SUPERSEDED REFERENCE (2026-08-03, after the INSTRUMENT WAVE)** — kept ONLY because it measured legs the
  current run does not (the LEGACY suites and the serial `guard.sh`), and for the instrument notes below it.
  ⛔ Its greenfield numbers are stale; read them as history, never as the baseline.
  FULL greenfield Conformance **4180 / 4180, zero skipped, NOTHING red** (13 m 03 s) · greenfield Unit
  **3634 / 3634, zero skipped** (3628 + the 6 new `ProcessObservationDriftTests`) · characterization **33 / 33** ·
  GnuCOBOL differential **1323 cases**, the same four totals — **0 per-case flips**, and **0
  `NO_COMPILER_EVIDENCE`** (the new A12e bucket found nothing evidence-free in a full run). Separately measured:
  legacy Unit **1203 / 1203** · legacy Integration **503 / 504 (1 skipped)** · `guard-fast` NIST **353 MATCH /
  0 REGRESSION** with **NIST AUDIT CLEAN — population 376/376, missing 0, duplicate 0, manifest 0, unexpected 0,
  no-verdict 0** · `guard.sh`'s serial loop **ALL GREEN + audit clean** over a subset covering every verdict class,
  and its evidence branches proven in the failing direction (`COMPILE FAILED` carrying its own reason inline, plus
  `NO MANIFEST ROW`). **ZERO harness observation events across the whole battery** — the only entries in
  `COBOLNET_HARNESS_LOG` are the drift tests' deliberate timeouts.
  ⭐ **THE AUDIT LINE IS THE ONE THAT MATTERS NOW, NOT THE MATCH COUNT.** "353 MATCH" was previously a number a
  human compared against memory, which is exactly how a run at 352 passed as green. The audit compares
  PER-PROGRAM against `tests/nist/corpus.tsv` and fails on a missing, duplicated, stray, or unexpected verdict.
  ⭐ **AND THE HARNESS NOW RECORDS ITS OWN RELIABILITY.** Every retry / non-observation appends to
  `COBOLNET_HARNESS_LOG`, so the determinism evidence §11 A12 asked for accrues on ORDINARY runs instead of
  needing dedicated repeat runs. Read that file before believing — or disbelieving — a red.
  ⚠ **THE CONFORMANCE TOTAL MOVED 4164 → 4180 ACROSS ONE SESSION** because each landed fix ships its goldens —
  do not treat any of these as constants. What must hold is **zero failures, zero skipped**, and a per-case
  differential diff of zero UNEXPLAINED flips.
  ⚠ **THE UNIT COUNT'S 972 → 3628 JUMP IS TWO DRIFT TESTS, NOT A WAVE.**
  `FloatQuantizeHeadroomDriftTests` proves its invariants over every legal (integer-digits, scale) pair a PICTURE
  can present (528 pairs × 5 theories) and `RefModCategoryDriftTests` adds 13. Wall-clock is unchanged (~2m 30s).
  ⛔ **AND A GREEN `guard-fast` IS NOT EVIDENCE UNTIL §11 A12c/A12d/A12e CLOSE — see the cautions below.**
  ⚖ **THE ADJUDICATION'S OWN ORACLE EVIDENCE, since it changed accept/reject:** NIST stayed **entirely green**
  across the flip — of 4166 cases the ONLY three reds were the three artifacts encoding the old premise — and the
  GnuCOBOL differential moved **0 of 1323 cases**. ⚠ Read that second number honestly: 0 flips proves the
  tightening moved nothing the GPL corpus COVERS, not that GnuCOBOL endorses the reading; their suite contains
  no numeric-edited arithmetic operand at all, and files de-editing exclusively under MOVE.
  · GnuCOBOL differential (at PB13) **1323 cases, ONE
  per-case flip, ATTRIBUTED, 0 unexplained** — `syn_functions.at :: invalid formats w/ DECIMAL-POINT IS COMMA`
  moved `WE_ACCEPT_THEY_REJECT → AGREE_REJECT`, i.e. a FIX; it is **PB11's** `COBOLNET1631`, not PB13's, proved
  by `git log -S` putting that code in `21c39c3d` which `merge-base --is-ancestor` confirms predates this
  session, and by PB13 touching no file that can emit it. The stored per-machine report simply predated PB11.
  New totals **559 WE_REJECT_THEY_ACCEPT · 487 AGREE_ACCEPT · 176 AGREE_REJECT · 101 WE_ACCEPT_THEY_REJECT**.
  ⚠ **THE UNIT COUNT JUMPED 972 → 3615 AND THAT IS ONE TEST CLASS, NOT A WAVE.**
  `FloatQuantizeHeadroomDriftTests` proves its invariants over EVERY legal (integer-digits, scale) pair a PICTURE
  can present — 528 pairs × 5 theories. It costs no wall-clock (2m 09s → 2m 09s). Do not read the delta as
  coverage growth elsewhere.
  ⚠ **HISTORICAL, for the delta:** the pre-PB13 numbers were Conformance **4164 / 4164** · Unit **972 / 972**.
  `guard-fast.sh` **=== ALL GREEN ===** with NIST **353 MATCH / 0 REGRESSION**, legacy Unit **1203 / 1203**,
  Integration **503 / 504 (1 skipped)** — **MEASURED at PB13, not carried.** ⛔ **Its FIRST run was a FALSE RED**
  (`IC222A: COMPILE FAILED`, 352 MATCH / 1) and it was run down rather than dismissed: IC222A compiles clean
  SERIALLY, the re-run is 353/0, and PB13 provably cannot reach it — `guard-fast` drives the LEGACY
  `cobolsharp.dll`, whose closure is `CobolSharp.CLI → CobolSharp.Compiler → Cobol.Net.Frontend`, while this diff
  touches only `Cobol.Net.Compiler` + `Cobol.Net.Runtime`. The MECHANISM is in the script and is now **§11 A12b**.
  *(The 42 NIST IF1xx intrinsic programs that DO exercise PB13 run through the GREENFIELD compiler inside the
  Conformance leg above, not here.)* · GnuCOBOL differential
  **1323 cases, 0 unexplained flips**, totals **559 WE_REJECT_THEY_ACCEPT · 487 AGREE_ACCEPT · 175 AGREE_REJECT ·
  102 WE_ACCEPT_THEY_REJECT** — RE-RUN AT PB9 with a per-case diff showing **0 flips in either direction**, so
  the totals stand unchanged. ⚠ That is a clean result, not a strong one: the GPL corpus does not exercise
  the keyword-omitted reserved-name form at all, so the differential had nothing to say about PB9 either
  way. The internal battery is the evidence there.
  ⚠ **THE DIFFERENTIAL MOVED 19 CASES AND EVERY ONE IS ATTRIBUTED** (the stored per-machine report was a day
  stale, so the diff spans PB1 · PB6 · PB7 · PB8 together, not PB8 alone): **17 FIXES**, of which **13 name
  reference modding outright** (`FUNCTION UPPER-CASE/LOWER-CASE/REVERSE/SUBSTITUTE/TRIM … with reference
  modding`, the three FORMATTED-* families) — the external corpus confirming PB8 — plus CURRENT-DATE and
  WHEN-COMPILED (PB7) and `Intrinsic functions: argument type` (PB1). The **2 AGREE→divergence flips are PB6's
  two PERMANENT deliberate divergences** already recorded below, not regressions.
  ⚠ **THE CONFORMANCE TOTAL MOVES WITH EVERY GOLDEN, so do not treat 4150 as a constant** — it is the number at
  the last measured commit and each landed fix adds its fixture. What must hold is **zero failures, zero
  skipped**. Conformance takes ~11–16 min; **run the long legs ONE AT A TIME** (a `--no-build` run alongside a
  rebuilding guard produces no verdict at all).
  ⚠ **The two permanent differential divergences are DELIBERATE**: `CALL BY VALUE` with an alphanumeric and with
  a national operand stay `WE_REJECT_THEY_ACCEPT` forever, because §14.9.4.3 SR22 excludes them and GnuCOBOL
  accepts them as an extension (PB6). They are not a residual regression; do not "fix" them back.
  ⛔ **Read the VERDICT LINE, never the exit code.** `guard-fast` reported exit 1 on a fully green run because
  the invoking command chain ended in a `grep -c` that matched nothing. Redirect the full output to a file, then
  `grep 'ALL GREEN'` and `grep -icE 'crash|abort|Failed: *[1-9]'`.

- **⛔ EVERY SPEC CITATION IS VALIDATED MECHANICALLY — `python scripts/spec/cite.py --check <clause> "<text>"`.**
  CLAUDE.md rule 1 requires it. The failure mode is INHERITING a citation, not inventing one: a queue entry
  carries a §, its quoted text is genuinely in the standard, and the clause NUMBER is never re-derived. Two CA10
  citations were wrong exactly that way (real: §8.4.2.3.4 GR2 and §13.18.38.4 GR7); CA37/CA38's were one level
  short (§14.9.39 → §14.9.39.4). The tool reads the MARKDOWN, never `spec-rule-catalog.json` — the catalog has
  no block for a prose clause and its ordinal stops tracking the printed number once a rule has sub-letters.
- **⛔ VALIDATING THE RULE TEXT IS NOT VALIDATING THE PREMISE.** CA12 was REFUTED after being implemented: its
  GR3g/GR4b citations were correct, but a Format-3 `USE` cannot carry `GLOBAL` at all (§14.9.49.2 gives
  `[ GLOBAL ]` to Formats 1 and 2 only — confirmed by RENDERING printed page 804), so GR4b can never select one
  and the outward walk it asked for is unreachable. I had also told the owner the I-O/EC asymmetry was "one rule
  with two behaviours"; that was wrong and is corrected in the queue, the deep-dive and DEVLOG 1091. **Ask what
  the construct can SYNTACTICALLY BE, not only what the rule says about it.**
- **A finding's stated scope is an estimate, not a ceiling** (CLAUDE.md rule 5). V57 said "binder-only" and
  needed a runtime PUSH ALL/POP ALL; V55's "at emit time" was architecturally wrong (codegen owns no
  `TurnState`); CA10 needed a shared emit-signature change. Expect to correct the recipe, and update the doc.


- **Mission (owner decision D13):** 100% CONFORMING per ISO §4.2.16 across ALL FOUR editions (85/2002/2014/2023) —
  mandatory core complete plus every required implementor-documentation item; optional modules may remain
  documented non-support. **Done = the P14 Step-0 traceability inventory at zero GAP.**
- **Releases (D14):** v1.0 = the P15 exit (100% conforming ×4). P16 CIL = v2, off the conformance critical path.
- **Effort model (§11 A10; refresh at each phase close):** P14 ≈ 12–22 sessions · P15 3–5 → **v1.0 ≈ 19–33
  sessions**; P16 (v2) 9–16, high variance. Swing factors: the OO wave and the inventory GAP count.
- **Diagnostic codes — claim the next free code by RUNNING `session-probe.ps1`, never by reading a list.** It takes
  the ceiling of BOTH scans (src grep + `DiagnosticCatalog`). Two channels legitimately diverge: compiler-channel
  raw `Edition.Error` codes are NOT catalog descriptors (ledger V11 queues folding them into the
  `EveryEmittedCode_IsACatalogDescriptor` drift test), so src-max ≥ catalog-max is the expected steady state;
  catalog-max > src-max is the real anomaly (an orphan descriptor — reconcile before allocating). **Released or
  reserved mid-band holes — do not "fill" them:** 1550–1552 · 1589–1590 · 1598 (no §14.9.28.3 SR backs
  operand-exclusivity) · 1602/1603 (XS-POP/PUSH — they ride the unimplemented `>>POP`/`>>PUSH` wave) · 1609
  (subsumed by COBOLNET0712) · 1613 (XS-DELETE-FILE-MULTI — unreachable until the 2023 multi-file `DELETE FILE`
  grammar lands). Fixed anchors: introduction gate 0900 · new-reserved-word 0901 · obsolete 0903 · §4.2.6
  recognize-and-name warning band 1560 · staged-not-implemented 0899. **A wave that does not need its whole
  reservation releases the remainder in its own §0 edit rather than leaving a hole.**
- **⛔ The transcription is under active repair — see NEXT item 2.** `specs/ISO_COBOL.md` is lossy in a *directed*
  way: every normative defect found so far is FALSELY RESTRICTIVE. Treat any grammar rule or diagnostic derived
  from a FIGURE as suspect until that figure has been checked against the printed page. The reproduction question
  is settled: the standard's own Introduction permits reproducing it in whole or in part provided the
  acknowledgment paragraphs are carried (they are, at page 28), and the copy is licensed — so a page that cannot
  be transcribed FAILS LOUDLY with a `TRANSCRIPTION-FAILED` marker and is never summarised.
- **⛔ Spec DIAGRAMS: render the PDF page whenever a general format is load-bearing** — `pwsh`/`python3
  scripts/render-spec-page.py <page>` (anchor `page-N` == PDF page N). `specs/ISO_COBOL.md` is OCR'd from the
  printed standard: rule TEXT survived faithfully, DIAGRAMS did not. All 244 general-format diagrams were audited
  and 226 corrections applied (specs submodule `763a521`), but the failure mode was always FALSELY RESTRICTIVE
  syntax — legal source made to look illegal. Never derive a general format from prose alone, and treat any
  grammar rule, diagnostic or conformance expectation derived from a FIGURE before 2026-07-19 as suspect.
- **Open GAPs carried into P14** (each a live COBOLNET0899 staging or a named gap — none is a silent hole):
  PICTURE-EDITING multi-char-literal + floating render · VALUE Format 2 multi-dimension odometer and
  subordinate-item table VALUE · national-key file collating · `>>SOURCE FORMAT`/free-form not rejected at
  `--std cobol85` (a VERSION-matrix/VCR gap). ⛔ Do NOT assert that out-of-range table occurrences default to
  spaces/zero — §13.18.63.4 leaves them UNDEFINED.
- **Known-unenforced syntax rules, scheduled to P14 Step 0b** (the SR census, §11 A2): `OCCURS` on a level
  01/66/77/88 entry (§13.18.38.3 SR1a) and `RENAMES` naming a level 1/66/77/88 entry (§13.18.45.3 SR5) — we accept
  source the standard forbids, at all four editions.
- **Other live registers — consult them, never duplicate them here:** §3 execution model · §8 forward-residue
  ledger · §9 verification commands + corpus mechanics · §11 analysis backlog (ten known-missing analyses, each
  with a scheduled home — rescheduling a row is an §0 edit, never a silent drop) · §12 risk register.
- **Standing rules:** the ⛔ NON-NEGOTIABLE PROCESS RULES live in `CLAUDE.md` (spec-first with cited §s · implement
  from the deep-dive design docs · complete, never test-scoped · docs current · root-cause, never a workaround).
  Design SSOT = `docs/COBOLNET_DESIGN.md` + the `docs/rearchitecture/DESIGN-*.md` deep-dives. History = `DEVLOG.md`.


---

# PART I — THE PLAN

## §1 North-star architecture (the end state)

A single commercial-quality COBOL→C# compiler (`cobol.exe`) that translates COBOL into idiomatic, typed-native C#
compiled by Roslyn, implementing full ISO/IEC 1989:2023 plus correct 85/2002/2014 as "four compilers in one." **FIVE
assemblies with clean layering:**

1. **`Cobol.Net.Editions`** — a new lowest leaf both Frontend and Compiler reference: an immutable `EditionInfo` (the
   single `DialectLevel` source), a `constructs.json`-generated `ConstructRegistry` + `ReservedWords`, a first-class
   `DiagnosticDescriptors` registry (every code = one rule, ISO §, severity, suppress-key), and one
   `EditionSeverityPolicy` — the root fix for today's frontend/compiler edition-metadata duplication.
2. **`Cobol.Net.Frontend`** — a **superset** single-ANTLR grammar: the edition `{isXXXX()}?` gates are REMOVED so
   every construct parses at every `--std` (a committed-match construct-id **annotation** local to each version-gated
   rule carries identity forward; a forward, identity-carrying lookahead survives ONLY where a construct is genuinely
   ambiguous across editions), ONE generated context-sensitive word set, no dead grammars / no JSON-XML,
   `ReservedWordEditionHints` **deleted**, and a typed `Cst` façade replacing ~336 `GetText()` walks. (See
   `DESIGN-version-conformance-pipeline.md`.)
3. **`Cobol.Net.Compiler`** — a REAL Binder phase: a manifest-driven `IBindPass` pipeline whose `Requires`/`Produces`
   DAG is asserted at startup, producing an immutable `BoundCompilation` over a pure `Model/` folder where storage
   representation is one computed `StorageForm` discriminator (killing the 7-site mutable `StoreAsImage`) and name
   lookup is one scope-aware `SymbolTable`; a source-generated exhaustive visitor over the sealed bound tree makes a
   missing arm a COMPILE error; CodeGen decomposes into `ProgramEmitter` + per-verb emitters + renderers over an
   immutable `EmitContext`, with a structural (non-string) `Place` rendered by a Roslyn-side `PlaceRenderer` behind an
   **`ICodeGenBackend`** seam and a typed `RuntimeApi` façade. **The binder is edition-AGNOSTIC** (zero `Check` calls save the UDF exception; the complete remainder is the DESIGN-version-conformance-pipeline §1.1 ledger);
   edition conformance is ONE `VersionConformancePass` over the bound tree, and **bind and emit are SEPARATE phases the
   driver gates** so codegen never runs on an errored tree (`DESIGN-version-conformance-pipeline.md`). Pipeline:
   `parse → bind (edition-agnostic) → VersionConformancePass → emit-if-clean → backend`.
4. **`Cobol.Net.Runtime`** — the typed-native value library plus one `RunUnit` context owning all run-unit state and
   one `FileConnector`/`FileRegistry` replacing the three duplicated file machines.
5. **`Cobol.Net.Cli`**.

The frozen legacy byte engine is deleted (Cut 2), its differential net first baked into committed greenfield goldens;
a self-standing golden + 403-census + negative-corpus + version-matrix guard replaces `guard.sh`; and the full ISO
§4.2.16 conformance documentation set is published. Hard invariants upheld throughout: typed-native data only (`byte[]`
confined to the sanctioned Tier-C REDEFINES / file boundary), spec-first correctness, one canonical mechanism per job,
no god classes, a **backend-neutral bound tree so a CIL backend is droppable in (§3)**, and a battery that is green at
every phase boundary.

## §2 Guiding principles (non-negotiable through the migration)

1. **Green at every boundary; migrate by prove-then-delete.** Each phase is independently shippable to `main` and
   leaves the FULL battery green (greenfield conformance + unit + characterization + the NIST legacy guard — the
   current counts live in the STATUS banner). For every mutable flag or
   duplicated computation being retired, compute the new form first, cross-check it against the old across the whole
   corpus, and only then delete the old — never on faith.
2. **Typed-native data ONLY.** A COBOL record IS a C# record struct; an elementary item IS a native field. A `byte[]`
   appears only at the genuine REDEFINES Tier-C codec or a file boundary. Never regress to a byte `ProgramState`.
3. **Spec-first.** Behavior is defined by ISO/IEC 1989:2023 (`specs/ISO_COBOL.md`), derived and cited by § BEFORE
   implementing. The legacy oracle + NIST goldens are regression nets with known holes, not authority. Implement each
   feature COMPLETELY to the spec + its deep-dive; tests verify, never scope.
4. **One canonical mechanism per job.** `StorageForm` (not scattered `Pic`/`StoreAsImage`/Tier inference), one
   `RecordLayout` (not 4 offset/width copies), one `CobolLiteral.Decode`, one `PhraseBlocks`, one `SymbolTable`
   lookup, one `IDiagnosticSink`, one `EditionSeverityPolicy`.
5. **Make ordering and completeness STRUCTURAL, not conventional.** The bind pipeline is a manifest of named passes
   with a `Requires`/`Produces` DAG asserted at construction; bound-tree dispatch is a source-generated exhaustive
   visitor so a forgotten arm is a compile error, not a runtime `LoudStmt`.
6. **No god classes.** Real collaborator classes over an injected context (`BinderContext`/`EmitContext`), not
   size-driven `sealed partial class` slicing. Folder = sub-namespace; file = its single public type.
7. **Ownership and immutability.** Passes mutate only through a write handle; downstream phases consume an immutable
   `BoundCompilation` / read-only views. Eliminate every cross-layer write-back (**the emitter must never mutate the
   binder's data model** — the `MarkStoreAsImage` write-back this principle eliminated was the exemplar breach).
8. **Backend-neutral bound tree (see §3).** No C# string, Roslyn `Syntax*`, mangled identifier, or format literal ever
   enters a bound node or `Place`. Neutrality is *proven by a second backend*, not merely asserted.
9. **Four editions in one, harness-validated.** Every construct's (edition × behavior) matrix is proven by fixtures;
   VCR status is DERIVED from the harness, never hand-ticked; every gate ships a negative witness; a too-new or removed
   construct fails LOUD with a named construct + edition, never a generic parse error.
10. **Interleave; keep sequencing risk behind the safety net.** Stand up characterization/oracle-bake FIRST; land the
    independent early feature track (version-gating) on the new editions framework; then the deep data-model/binder/
    emitter rearchitecture; then the big feature waves (OO, national, M3, M4) ONLY on the rearchitected foundation.
11. **Every post-85 feature ships in ONE change set** with its conformance golden, its reject-at-earlier-edition matrix
    rows, and its negative case; the subsystem deep-dive + `DOC_INDEX` update in the same commit. Recognized-but-not-
    implemented is a named diagnostic descriptor, never a silent wrong answer.

## §2.1 First-class mandate: SELECTABLE CODE-GEN BACKENDS (Roslyn ↔ CIL)

The compiler **must** support swapping the Roslyn/C# backend for a direct CIL/IL backend (owner goal
`project_dual_backend_goal`), selected at will, **without touching the frontend, binder, or bound tree**. This is not
aspirational — it is a scheduled deliverable and a *cross-cutting architectural driver*, not a late add-on:

- The **bound tree is a backend-neutral IR** — a pure semantic model (resolved symbols, a *structural* `Place` lvalue,
  categories, scaled-integer numeric facts). It must contain **no** C# strings/identifiers, Roslyn `Syntax*`, `using`
  decisions, .NET type names, or format literals. Phases 05/06/07 must each enforce this (see
  `DESIGN-backend-abstraction.md` for the exact contract + the "backend-neutrality" test).
- Code generation is **one visitor per backend over that IR**, behind `ICodeGenBackend`; `RoslynBackend` and the future
  `CilBackend` share the model walk, a neutral `RuntimeAbi` runtime-op vocabulary + a shared `NameMangler` (both call
  the same `Cobol.Net.Runtime`), and the name-mangling rules. **Backend = Mono.Cecil** (chosen for real portable-PDB /
  sequence-point support — Reflection.Emit lacks it — plus explicit metadata authoring for EXTERNAL/GLOBAL/cross-CALL
  + OO hierarchies, MIT license, decades-mature; isolated in a leaf `Cobol.Net.Backend.Cil` assembly so the default
  Roslyn path stays Cecil-free). Reflection.Emit is used ONLY for the throwaway seam-proof.
- The **seam-proof lands as PHASE-16 Milestone 0** — a `NullBackend` + a tiny in-box Reflection.Emit `DisplayBackend`
  + the executable backend-contract test — proving `ICodeGenBackend` is real and the bound tree carries no C# *by a
  second consumer* before the full backend build-out. (Originally scoped as a post-P07 milestone; P07 closed at
  Step 12 without it, and `DESIGN-backend-abstraction.md` formalizes it as PHASE-16 M0. The bound-tree neutrality
  invariant it proves is guarded in the interim by the §7 I1 `BackendNeutrality` grep gate.) **Phase 16** then
  delivers the full `CilBackend`, gated by a **backend-equivalence harness** (the golden corpus byte-identical across
  `--backend roslyn` and `--backend cil`).
- ⚠ **Neutrality is broader than `Place`.** The backfill review found the leak reaches into `BoundTree.cs` itself
  (bound *statement* nodes carrying C# path/identifier strings — `BoundSearch.IndexField/DependCount/DynTable`,
  `BoundSetCapacity.TablePath`, `CapacityRegisterPlace.TablePath`, `BoundIndexRef.IndexField`, `BoundMethod.CsName`)
  and into the whole **program skeleton** (PC dispatcher, `ICobolProgram` ABI, USE/`__IoCheck`, GLOBAL bridges, entry
  wrapper, file registration) — none of which the original P07 covered. `DESIGN-backend-abstraction.md` folds these
  in as required PHASE-05/06/07 additions + a program-skeleton neutralization checklist (see §7).

Full design: `docs/rearchitecture/DESIGN-backend-abstraction.md` + `DESIGN-codegen-backend.md`. Step-by-step:
Part II §PHASE-16 of this document.

## §3 EXECUTION MODEL — tiered testing + batching + parallelism (owner-directed; use it every session)

- **Tiered testing.** *Wave-local gate* (~2–3 min): fresh `CobolSharp.sln` build → characterization +
  `CorpusRunnerTests` filtered to the wave + targeted unit tests + a CLI probe. *Comprehensive gate*
  (~15–20 min): FULL greenfield Conformance + (when forced) the FULL legacy guard — once per BATCH of waves and
  MANDATORY before any merge to `main`. Forcing guardrails: any shared-`.g4`/preprocessor/lexer change → full
  legacy guard; any bound-tree shape change or shared-infra refactor → full Conformance; any `#if DEBUG`/
  `[Conditional]` → a `-c Release` leg; enabling a shared-corpus golden → the legacy suite too. ⚠ An
  acceptance/semantics change gates with the FULL Conformance project, never a CorpusRunner-only filter (the
  `04c32a93`/`cf1fcaa2`/GOBACK-GR3 lessons — three recurrences).
- **Batching.** Land 3–4 INDEPENDENT greenfield waves sequentially in ONE tree, gate each wave-local, ONE
  comprehensive battery per batch, then commit each wave SEPARATELY off the green tree (verdict-gated) + push.
- **Parallelism.** RELIABLY parallel: scouts, adversarial reviews, mechanical/disjoint bulk — fan out ≤10-agent
  batches and DURABLY FOLD+COMMIT each batch's results before launching the next (the 2026-07-19 review proved
  this survives spend-limit outages). KEEP SERIAL: waves sharing EC/gate hot files; the grammar batch; the
  phase chain 13→14→15. Pre-allocate diagnostic ranges before any parallel fan-out. Spec-first FEATURE work
  stays supervised.
- **Scope-control rule (D15).** Every newly-found work item is TIERED at fold time: CONFORMANCE-BLOCKING /
  QUALITY / POST-v1.0. Only the first may gate a phase; the other two are scheduled without extending exit
  criteria. This is the counterweight to the review/inventory ratchet — analyses ADD knowledge, not
  automatically deadline-scope.
- **Guard-flake rule:** a guard verdict naming a file-I/O suite (SQ/IC/IX/ST/OB) under JOBS=32 is the known
  environmental flake class — re-prove by SOLO rerun before treating as real.


## §4 Phase roadmap (dependency-ordered; tick on completion)

Tracks: **F**=foundation · **R**=rearchitecture · **I**=feature/ISO · **C**=cleanup. Keep the battery green at every
phase boundary.

| ☐ | Phase | Trk | Risk | Deps | Title | Doc |
|---|-------|-----|------|------|-------|-----|
| ✅ | 00 | F | LOW | — | Migration safety net (characterization harness, oracle bake-out, corpus consolidation, ref caching) | Part III record |
| ✅ | 01 | F | MED | 00 | Mechanical namespace rename + dead-grammar / JSON-XML removal | Part III record |
| ✅ | 02 | R | MED | 01 | `Cobol.Net.Editions` leaf assembly + first-class diagnostic registry | Part III record |
| ✅ | 03 | I | HIGH | 02 | Version-**conformance pipeline** (superset parse + ONE two-arm gating pass; **residue-first**) + harness-driven VCR audit — DONE 2026-07-10, all 9 exit criteria hold; binder edition-agnostic save the §1.1 exception ledger (completed by Exec Step E) | Part III record · `DESIGN-version-conformance-pipeline.md` |
| ✅ | 04 | R | MED | 02 | Frontend consolidation (generated word-set + shared literal fragments + typed `Cst` façade) — **DONE (byte-neutral; all 5 exit criteria hold). D10 (SUBSCRIPT-mode removal) RELOCATED to PHASE 15 §"CUT 2.5"** (blocked until the legacy `SUB_*`/`SubscriptEntryContext` consumer is deleted at P15 Cut 2) | Part III record |
| ✅ | 05 | R | HIGH | 00,02 | Unified data model (`StorageForm`, `Model/`, `RecordLayout`, pass scaffolding) — **DONE (all 7 exit criteria hold; deviations in the PHASE-05 ledger). The `StoreAsImage` FLAG is gone (`Storage` computed once, the name = the read-only projection); `RecordLayout` the ONE width/offset authority (§13.18.44.3 SR8 ENFORCED — COBOLNET1539); `Binding/Model/` + `PictureAnalyzer`/`StrongTypeModel`; sentinels → `DataItem.Pending`; `RedefinesClass.Classify`; apostrophe goldens covered** | Part III record |
| ✅ | 06 | R | HIGH | 05 | Real binder phase (manifest pass pipeline, `SymbolTable`, immutable `BoundCompilation`) — **DONE (all 6 exit criteria hold; deviations in the PHASE-06 STATUS ledger)** | Part III record |
| ✅ | 07 | R | HIGH | 06 | Exhaustive visitor dispatch + binder/emitter god-class decomposition — **DONE (Steps 1–12: both god classes dissolved; structural `Place`; FUNCTION-arg grammar + the IntrinsicRenderer static-channel deletion — as-landed record in the PHASE-07 STATUS)** | Part III record |
| ✅ | 08 | R | MED | 00 | Runtime library reorg (`RunUnit`, `FileConnector`/`FileRegistry`, role-based folders) — DONE 2026-07-15 | Part III record |
| ✅ | 09 | I | HIGH | 04,07 | M2 OO rearchitecture (`Oo/` + `OoDriver`) + mandatory 2002 OO completion | Part III record — DONE 2026-07-16 |
| ☑ | 10 | I | MED | 05,08,09 | M2 residual catalog (national/boolean, pointers, UDF, file-2002, RW/CONSTANT/concat) — DONE 2026-07-17 | Part III record |
| ☑ | 11 | I | MED | 10 | Deferred-intrinsics backlog → zero (DONE 2026-07-17) + Tier-C rejection single-sourced; the confined-byte[] codec (Step D) DEFERRED as a scheduled increment | Part III record |
| ☑ | 12 | I | MED | 10 | M3 (COBOL-2014) deltas — DYNAMIC LENGTH, IEEE float USAGE family (binary32/64 native; binary128/decimal processor-dependent non-support — IEEE-fidelity inversion corrected), >>PROPAGATE intro-gate, TYPE TO re-anchor — DONE 2026-07-17 (E PICTURE + FUNCTION-POINTER runtime staged 0899; >>PROPAGATE semantics → P13) | Part III record · `PHASE-12-scout-notes.md` |
| ◐ | 13 | I | HIGH | 11,12 | M4 (COBOL-2023) deltas + EC remnants + behavior rows — the grammar batch + Wave-D + Track ③ MERGED TO MAIN 2026-07-22 (`1f56f572`); residue on `phase-14` (F3-in-a-method + §24 fix-queue), live state in §0. DONE: Step-1 audit · Wave B EC-SIZE · Wave C 8/10 grammar constructs · the GRAMMAR BATCH 4/7 (Wave H · RW SUPPRESS · COLLATING §12.4.5.7 · SUPPRESS WHEN §12.4.5.6) · **STOP/GOBACK exit-code (VCR 75)** · **EC-BOUND-OVERFLOW + REF-MOD raise (EC-BOUND surface CLOSED)** · **Wave F USE FOR DEBUGGING (VCR 7.17, parallel-worktree + adversarial-review)** · Wave G **8 pin-to-spec dispositions** · Wave H CONFORMANCE.md/SCREEN warning · Wave I partial review. Wave G CLASS A + REF-MOD-ZERO-LENGTH + Wave E (incl. the VCR-15 EC-EXTERNAL raises) COMPLETE 2026-07-19; **the comprehensive plan-vs-spec review is VERIFICATION-COMPLETE (DEVLOG 906–916): the ledger `PHASE-13-plan-vs-spec-review.md` §24 = the 10-tier prioritized fix queue — the fix SSOT**; fixed during the cycle: both diag collisions (1573→1576, 1518→1577) + drift guards, CONFORMANCE.md restoration + locale row + the A.4 §5 section, citation sweeps, VCR-16 strength half, GOBACK GR3, WriteFill AllowZeroLength. REMAINING: per the D16 close-line — **the live worklist is §0 ONLY (the single-write rule)**; closes as **M4-beta**. Worklists: §0 (the live list) + the two P13 scouts (working designs) + the review ledger §24 (the fix queue); the audit was re-verified by the review and deleted | §0 worklists + the review ledger |
| ☐ | 14 | I | HIGH | 03,13 | **Step 0: the FOUR-EDITION SPEC-TRACEABILITY INVENTORY (the D13 definition-of-done instrument — every clause/statement/function/directive/format × edition mapped LIVE/STAGED/DISPOSED/GAP with evidence; batched-agent sweep, durably folded per batch; + Step 0a authority-sufficiency + Step 0b SR-enforcement census)** + **the D17 OO MANDATORY-SURFACE OPENING WAVE (parallel lane)** → matrix closure (VCR zero-todo) + in-repo greenfield guard + one-time equivalence proof + **Step 12 perf gate**; §11 campaigns A3/A4 run here and gate P15 | Part II §PHASE-14 |
| ☐ | 15 | C | MED/HIGH | 14 | **= v1.0 (D14: 100% conforming ×4).** G8 legacy retirement (three cuts) + §4.2.16 conformance docs + runtime namespace flip + **§"CUT 2.5" D10 SUBSCRIPT-mode removal** (relocated from P04; runs after Cut 2 deletes the legacy `SUB_*` consumer) | Part II §PHASE-15 |
| ☐ | 16 | R/I | HIGH | 07 (seam) ; 08 (full) | **= v2 (D14 — off the conformance critical path). CIL/Cecil backend + backend-neutrality proof** (`--backend cil`, equivalence harness) | Part II §PHASE-16 |

> **Phase 16 sequencing:** its cheap *seam-proof* milestone (Milestone 0 — proving `ICodeGenBackend` is real and the
> bound tree carries no C#) was originally scoped for the post-Phase-07 slot but deferred to PHASE-16 M0 (P07 closed
> at Step 12 without it); the interim guard is the §7 I1 `BackendNeutrality` gate. The full `CilBackend` build-out
> then proceeds behind the backend-equivalence harness. See `PHASE-16` for the exact milestone breakdown.

> **Version-conformance pipeline (cross-cutting driver, `DESIGN-version-conformance-pipeline.md`).** Like the
> dual-backend mandate (§3), the pipeline touches several phases — but **P03 owns DELIVERY of the ENTIRE pipeline,
> sequenced residue-first** (its Steps 12–15): Batch C (Step 12 — RETRY #4's six grammar predicate sites + the boolean
> family #2) → delete `ReservedWordEditionHints` (Step 13; the vendor JSON/XML COBOL0313 disposition relocates to
> `CobolErrorStrategy` as a token-keyed vendor hint) → the pipeline skeleton (Step 14, design §5 Stage 3): the
> `VersionConformancePass` over the bound tree that funnels ALL 88 compiler-embedded `ConstructRegistry.Check` call
> sites, absorbs and DELETES `EditionValidator` (its §8.9 reserved-word funnel moves into the pass), makes the binder
> edition-agnostic (zero `Check`s save the §1.1 ledger; the pass's parse arm walks the RAW tree — NO bound node carries a `.Syntax` back-reference, the BoundTree invariant), splits bind/emit in
> `CompilerDriver` (bind → pass → HALT on errors → emit), and re-points `CheckOnly` / `check-batch` / `EditionHarness`
> / the INV-1 continuity + INV-1-strong legs so their verdicts include pass diagnostics. The later phases PRESERVE and
> HARDEN it: **P04** (frontend) completes the superset grammar + the committed-match construct-id ANNOTATION convention
> (grammar actions + side-table storage keyed by parse context) + the D10 owner override (SUBSCRIPT-mode removal) —
> and re-introduces NO edition predicates; after P03 the only surviving predicates are the two load-bearing
> forward-detects (the openClause `{is2002() || retryPhraseAhead()}?` and the `boolExprAhead()`-based condition ENTRY).
> **P06** (real binder) makes the `VersionConformancePass` a NAMED pass in the `IBindPass` manifest — exit criterion:
> zero `ConstructRegistry.Check` calls in any `IBindPass` other than the conformance pass; `CheckOnly` runs the
> manifest through the conformance pass. **P07** preserves the bind-vs-emit separation through the emitter
> decomposition — exit criterion: emitters contain no edition gating, and emit is unreachable with non-empty
> diagnostics. **P13** (M4 wave): a NEW edition-gated construct = a `constructs.json` row + a superset grammar rule
> (stamped with its construct-id annotation, or a self-identifying bound node) + a `VersionConformancePass` rule + a
> negative fixture at EVERY earlier edition — NEVER a new parse-time edition predicate (unless a proven load-bearing
> ambiguity needing a forward-detect) and NEVER a binder-embedded `Check`. **P14**'s matrix-closure gates reference
> the `VersionConformancePass`, not `EditionValidator`. Each affected phase's scope note points back to the design doc.

## §4.1 — Execution resequencing (2026-07-11, owner-directed: TOOLING-FIRST foundation)

**Owner directive (2026-07-11, `[[project_leverage_antlr_roslyn_tooling]]`; evaluation
`docs/rearchitecture/EVAL-antlr-leverage-and-traversal.md`):** the §4 order is dependency-correct but **buries the two
highest-leverage TOOLING foundations too late**, so every phase until then pays the ad-hoc-walker / no-symbol-table
tax — the direct cause of the PHASE-05 `UsageCollectionPass` completeness bugs (6 missed statement types +
`DynTablePlace`). The bound tree had **no shared visitor** → **205 duplicated `case Bound` arms across ~5
bespoke hand-walkers** synced by a prose comment; the binder hand-rolls a 50-arm dispatch + 334 `GetText()` pokes; **no
`SymbolTable`**. **Resolution: front-load the tooling foundations; every later phase then LEVERAGES them instead of
hand-rolling** (multiple passes where useful — but generated/shared, not bespoke). This RE-SEQUENCES *execution order*;
it does NOT renumber the phases.

**New execution order (supersedes the strict §4 left-to-right for the R/foundation track):**

| Exec | Was | Work | Why here |
|------|-----|------|----------|
| **A ✅ DONE** | P7 Step 6 | **Source-generated exhaustive bound-tree visitor — DONE.** `Cobol.Net.Compiler.SourceGen` Roslyn incremental generator (`BoundVisitorGenerator` + `[BoundNode]` + generated `Accept<T>`/`IBound*Visitor<T>` + `BoundStatementTree.StatementChildren`). All ~5 bespoke walkers + the emitter dispatch + the renderers + `StoreKindOf`/`ArgExpr` converted; the 5 statement walkers recurse via `StatementChildren`. SYSTEMATIC AUDIT complete (every bound-node switch grep-classified, ISO-§-grounded). | **Independent of P5-remainder/P6** (walks the EXISTING bound tree); a missing arm is now a COMPILE error — killed the completeness-bug class. |
| **B ✅ DONE** | P6 | **PHASE 06 complete: `BinderDriver` → immutable `BoundCompilation`; the declared `GroupTail` manifest with the `VersionConformancePass` as NAMED terminal pass; one DAG + DEBUG watermark gate; 14 binder collections sealed `IReadOnly`; the lookup quadruple DELETED → the ONE scope-aware `SymbolTable` (`TryResolve`/`TryResolveIndex`/`IndexCellOf` over explicit `Scope`, per binder); `IOoBindHost`+`BindSession` = the P6→P9 seam.** All 6 exit criteria hold; deviations in the PHASE-06 STATUS ledger. | The name-resolution foundation the binder decomposition + every feature phase needs; replaces the ad-hoc quadruple over the `ByName`/name-index dictionaries. |
| **C ✅ DONE** | P5 Steps 6–14 | **PHASE 05 complete: the `StoreAsImage` FLAG deleted (collected facts → `StorageFormPass`; the name = the read-only projection of `Storage`); readers on `Storage`/`RecordLayout` (geometry copies collapsed; SR8 enforced COBOLNET1539); `Binding/Model/` move + `PlaceDecorator` + `StrongTypeModel` + `PictureAnalyzer` (PicInfo a pure value record; sentinels → `DataItem.Pending`); `RedefinesClass.Classify` the ONE tier-verdict mutator; ONE `UsageInheritancePass`; apostrophe goldens.** All 7 exit criteria hold; deviations in the PHASE-05 ledger. | Finishes the data-model migration ON the visitor + symbol table. |
| **D ✅ DONE** | P7 Steps 1–12 | **PHASE-07 complete: both god classes dissolved; structural `Place` (Step 11); FUNCTION-arg grammar + IntrinsicRenderer static-channel deletion (Step 12).** | Needed B (`BoundCompilation`) + A (visitor). |
| **E ✅ DONE** | (P2/P3 audit remediation, task #13) | **The ~19 inline edition gates FOLDED into the two-arm `VersionConformancePass`** (20 new registry rows, each in the version matrix); orphaned `GateId` scaffolding DELETED; the "edition-agnostic" over-claims corrected (the canonical §1.1 gating-exception ledger, DESIGN-version-conformance-pipeline). | Done ON the visitor + the now-clean pipeline. |
| **◐ F (NOW)** | P08–P16 | Runtime reorg, feature waves (M2/M3/M4), matrix closure, G8 legacy cut, CIL backend. | On the fully tooling-leveraged foundation. |

**Guiding rule going forward (`[[feedback_one_mechanism_per_job]]` + tooling):** any NEW tree traversal uses the ONE
generated/shared visitor (bound tree) or the ANTLR generated visitor/listener (CST) — never a fresh bespoke `switch`.
The PHASE-05/06/07 records (Part III) close A–D; §0 carries the live resume point; this section's banner
points at the current exec step. Keep this table + those STATUS lines + the §4 ticks in sync as each exec step lands.

## §6 Owner decisions (ALL RESOLVED — D1–D20)

**ALL RESOLVED by the owner (2026-07-07).** The rulings are recorded below. Where the ruling equals the recommended
default the phase docs already assume it (no change needed); the ONE exception is **D10** — an owner override that
EXPANDS PHASE-04 (see the note under the table).

| # | Decision | Owner ruling |
|---|----------|--------------|
| D1 | Namespace rename timing (`CobolSharp.Compiler.* → CobolNet.*`). | ✅ **Pull forward to Phase 01** (mechanical; reduces G8 to a pure deletion). |
| D2 | The edition/diagnostics home. | ✅ **New `Cobol.Net.Editions` leaf assembly** that both Frontend + Compiler reference. |
| D3 | JSON/XML (non-ISO; 0 spec occurrences). | ✅ **Hard-delete now** (Phase 01). |
| D4 | CIL/Cecil backend. | ✅ **Active — Phase 16 (Mono.Cecil).** |
| D5 | Structural `Place` (backend-neutral IR). | ✅ **Mandatory & not deferrable** (the §3 neutrality contract). |
| D6 | Binder output immutability. | ✅ **Fully read-only `BindModel`** — passes mutate only via explicit write handles; emit consumes read-only views. |
| D7 | Bound-tree dispatch exhaustiveness. | ✅ **Roslyn source generator** (compile-time exhaustive visitor; a forgotten arm fails the build). |
| D8 | Tier-C confined-`byte[]` codec. | ✅ **Implement in Phase 11 as its own increment**; single-source the loud rejection until then. |
| D9 | Binder decomposition granularity. | ✅ **One class per verb (~18)** over an injected `BinderContext`. |
| D10 | SUBSCRIPT lexer-mode. | ✅ **FULLY REMOVE** the lexer `SUBSCRIPT` mode + the binder subscript re-parse (a grammar-level `x(i)` rule) — the ambitious option, NOT the "defer" default. **Ruling stands; RELOCATED from PHASE 04 → PHASE 15 §"CUT 2.5"**: it is blocked by the frozen legacy compiler sharing `SUB_*`/`SubscriptEntryContext` until P15 Cut 2 deletes it. Designed in `DESIGN-frontend-grammar.md §9`. |
| D11 | Emitted `.g.cs`. | ✅ **Keep always-on** (a Roslyn-backend debugging artifact; the CIL backend emits IL, not `.g.cs`). |
| D12 | national (`PIC N`) / boolean (`PIC 1`) representation. | ✅ **The VALUE CARRIER stays CHARACTER-width** — one C# `char` per position on the shared `CobolString` substrate (D-N1/D-B1). ⚠ **NARROWED 2026-08-04:** that is a statement about the CARRIER, never about what an item OCCUPIES. A `USAGE BIT` item occupies BITS (§13.18.60.4 GR5) and images packed — design **D19**, fix-queue PB43. Its carrier is unchanged, which is exactly why every MOVE/compare/ref-mod path was untouched. |
| D13 | The conformance TARGET reading (2026-07-19). | ✅ **100% CONFORMING** per ISO §4.2.16: the MANDATORY core of each edition complete + every required implementor documentation item — with optional modules/processor-dependent elements permitted to remain **documented non-support** (§4.2.6/§4.2.7/Annex A; the CONFORMANCE.md dispositions are part of the deliverable, not a waiver). "Implement every optional module" is explicitly NOT the target; claiming one later is a new owner decision. The definition of DONE is the PHASE-14 Step-0 traceability inventory reaching zero-GAP/zero-UNKNOWN (every row LIVE, STAGED-with-schedule, or DISPOSED). |
| D14 | RELEASE MILESTONES (2026-07-19). | ✅ **v1.0 = the P15 exit** ("100% conforming, four editions" — the D13 target achieved and documented). **P16 (the CIL backend) = v2** — an architecture deliverable, explicitly OFF the conformance critical path. Intermediate nameable milestones: **M4-beta** = the P13 merge to `main`; **conformance-mapped** = P14 Step 0 complete (every GAP scheduled). |
| D15 | SCOPE-CONTROL RULE (2026-07-19 — the ratchet counterweight). | ✅ Every new finding/analysis output lands TIERED: **CONFORMANCE-BLOCKING** (may gate a phase exit) · **QUALITY** (scheduled work, never gates) · **POST-v1.0**. Only CONFORMANCE-BLOCKING items may extend a phase's exit criteria; the tier is assigned when the item is folded (review/inventory/campaign outputs alike). |
| D16 | THE P13 CLOSE-LINE (2026-07-19). | ✅ P13 closes on: the M4 feature waves (the grammar batch + Wave D) + review fixes to P13-LANDED code only + Wave I close. The REST of the §24 fix queue is formally RESCHEDULED into P14's GAP-closing work (the Step-0 inventory re-derives and re-tiers it). P13 is thereby bounded at ~4–6 sessions. |
| D17 | THE OO MANDATORY SURFACE (review V16–V19). | ✅ **Implement inside P14 as its own wave — the P14 OPENING FEATURE WAVE** (inline `::` invocation identifier form + disposition of the shipped non-ISO statement rule · object-view §8.4.3.5 · ACTIVE-CLASS/ONLY §13.18.60.2 · parameterized classes/interfaces + REPOSITORY EXPANDS/AS). Spec-first from a persisted scout per the proven wave pattern; sized 4–8 sessions; runs in a parallel lane with the Step-0 inventory. |
| D18 | HISTORICAL-STANDARDS ACQUISITION (the A1 trigger). | ✅ **PRE-AUTHORIZED if needed**: should Step 0a find per-edition facts underivable from the 2023 spec, acquiring the 1985/2002/2014 standards proceeds without a further decision round. The owner's stated expectation: the 2023 spec correctly identifies every statement's version across editions, so A1 is expected to CONFIRM derivability — the pre-authorization is the hedge, not the plan. |
| D19 | C4/C5 STAY IN THE GRAMMAR BATCH (2026-07-19). | ✅ PICTURE EDITING and PERFORM Format 3 are **retained in the P13 grammar batch** with their spec forks adjudicated inline, rather than deferred to dedicated waves. D16's close-line is therefore unchanged. (Adjudication outcome: 4 of the 5 claimed forks dissolved into spec — see D20 for the one that did not.) |
| D20 | `>>TURN` INSIDE AN EXCEPTION-CHECKING PERFORM (2026-07-19). | ✅ **FLAT BAN, SUPPRESSIBLE WARNING.** A `>>TURN` written anywhere lexically within a format-3 PERFORM — `imperative-statement-1` included, not just the handler phrases — draws a suppressible conformance warning citing §7.3.25.3 SR5; the program still compiles (§4.2.2 requires only an optionally-invoked warning for syntax-rule violations), and §14.9.28.4 GR14 sentence 4's semantics are still implemented for the accepted case. Use **ONE shared lexical-containment predicate** for TURN/PUSH/POP (singular-mechanism rule) — do not build a second region-partitioned predicate. **The REJECTED reading, recorded so it is not re-litigated:** the "narrow" reading holds that SR5's wording ("exception **processing** PERFORM") deviates deliberately from the sibling PUSH/POP bans ("exception **checking** PERFORM") and that §3.70 scopes "exception processing procedures" to the WHEN-phrase handlers, so SR5 would ban `>>TURN` only inside WHEN/WHEN OTHER/WHEN COMMON/FINALLY; it is further supported by GR14's implicit `PUSH ALL` + `TURN OFF ALL` … `POP ALL` bracket spanning exactly the handler region (a `>>TURN` there is unwound before END-PERFORM, which is what a syntax rule should forbid), whereas one in `imperative-statement-1` has both a specified effect (GR14 s.4) and specified survival (GR22). It was rejected because **Annex D.16.4 uses the phrase "exception-processing PERFORM statement" for the PUSH/POP ban** — which is stated normatively as "exception checking" and which both readings agree is whole-statement — so the two phrasings are drafting synonyms and the narrow reading's sole tie-breaker fails. Honest residual: D.16.4 is informative, and SR5's wording deviation remains unexplained under the adopted reading. |

> **D10 scope expansion → RELOCATED to PHASE 15.** The owner override stands: FULLY remove the
> lexer `SUBSCRIPT` mode AND the binder subscript re-parse, replacing them with a grammar-level subscript (`x(i)`) rule
> (retaining the minimal ISO §8.3.5-compelled WS mechanism per `DESIGN-frontend-grammar.md §9.4`). It was originally
> scoped into PHASE 04, but the design work (§9) showed it CANNOT land there: the frozen legacy compiler consumes
> `SUB_*`/`SubscriptEntryContext` (`ExpressionBinder.BindSubscriptEntry`), so the SUBSCRIPT machinery cannot leave the
> SHARED grammar until the legacy tree is deleted — which is **PHASE 15 Cut 2**. So D10 now runs as PHASE 15's
> **§"CUT 2.5"** sub-track (staged D10.1–D10.5, §9.5), immediately after Cut 2. PHASE 04 closed on Groups A–D. Recorded in
> `PHASE-04-…md`, `PHASE-15-…md` §"CUT 2.5", and `DESIGN-frontend-grammar.md §9`.

## §7 Backfill findings & roadmap refinements

Gaps surfaced by the re-run survey/critique units and the dual-backend track, folded back into the phase docs.

Six survey/critique units + the dual-backend track were re-run after schema failures in the main workflow. Their
verdict: **the 16-phase plan is sound and its diagnoses match the code almost line-for-line** — the refinements below
are corrections/extensions, not missing pillars. **Each executing session MUST apply the refinements tagged to its
phase** (detail + file:line in the cited `SURVEY-*.md` / `CRITIQUE-*.md` ROADMAP GAP CHECK sections).

- **R1 — Backend-neutrality is broader than `Place`** *(→ PHASE-05/06/07, PHASE-16).* The C#-leak reaches into
  `BoundTree.cs` itself (bound *statement* nodes carry C# path/identifier strings: `BoundSearch.IndexField/DependCount/
  DynTable`, `BoundSetCapacity.TablePath`, `CapacityRegisterPlace.TablePath`, `BoundIndexRef.IndexField`,
  `BoundMethod.CsName`) and into the whole **program skeleton** (PC dispatcher, `ICobolProgram` ABI, USE/`__IoCheck`,
  GLOBAL bridges, entry wrapper, file registration) — none covered by the original P07. Fold in the exact PHASE-05/06/07
  additions + the program-skeleton neutralization checklist in `DESIGN-backend-abstraction.md`; make `RuntimeApi` a
  neutral `RuntimeAbi` op-vocabulary, not a Roslyn-only façade. *(SURVEY-codegen-emitter.md, DESIGN-backend-abstraction.md)*
- **R2 — Frontend diagnostics stay stringly-typed** *(→ PHASE-02, PHASE-04).* PHASE-02's diagnostic registry is scoped
  to the compiler side; the frontend preprocessor emits the `07xx/08xx/09xx` band as bare string literals with no
  descriptor (`TurnDirectiveProcessor`/`ReferenceFormatProcessor`/`CopyProcessor`), so the "one diagnostic model" exit
  criterion could be declared met while a second stringly-typed path survives. Also: `ConditionalCompilationProcessor`
  (2002+ `>>DEFINE/IF`) is never edition-gated though its sibling is (correctness), and a `SourceLocation.Line`
  0-vs-1-based off-by-one between parser and preprocessor. *(SURVEY-frontend-plumbing.md)*
- **R3 — Run-unit statics under-scoped; the audit grep is blind** *(→ PHASE-08).* PHASE-08 limits Intrinsics to
  "Pow10+clock," leaving `CobolIntrinsics._random`, `CobolSort.Files`, `ExternalSwitches.States`, and
  `CobolTable.Scratch<T>.Slot` (a `ref`-returning process-global generic static) as un-homed run-unit state — so the
  "ONE owner of run-unit state" exit criterion is NOT actually met and concurrent/repeat in-process run units collide
  on RANDOM/SORT/switches. **The hidden-mutable-static CI grep is defeated by its own filter** (omits `Random`/
  `ConcurrentDictionary`; its `readonly .* =` exclusion skips exactly the `static readonly Dictionary = new()` pattern,
  and it cannot match a generic `static T`). Fix the grep and home ALL of them on `RunUnit`. *(SURVEY-runtime-io-control.md,
  SURVEY-runtime-value.md)*
- **R4 — Data-model: the OO cross-unit `StorageForm` harmonization is the real risk** *(→ PHASE-05).* The `StorageForm`
  bet is validated (COMP-3 is the same `long` as DISPLAY; the ~5 concurrent numeric representations the
  pre-rearchitecture engine reconciled only by mutating a public `StoreAsImage` flag are now one computed `StorageForm`). But the OO override-
  harmonize step (`CSharpEmitter.Oo.cs:694`) is a *pairwise cross-unit* reconciliation between two independently-bound
  formals — the design assumes it collapses to "one declarative per-item rule" and under-specifies it; it is the hardest
  parity risk. Also: the Tier-C `byte[]` codec (D8) stays unimplemented, so the "unified" model still cannot represent
  common mixed-usage REDEFINES; and the `Place`-from-`StorageForm` mapping must distinguish *phantom* items (CAPACITY
  register, RENAMES alias) from real stored ones. *(SURVEY-xcut-data-model-coherence.md)*
- **R5 — Encapsulation: pillars adequate, 4 refinements** *(→ PHASE-06/07/09).* The plan's `BinderContext` +
  `SymbolTable` + immutable `BoundCompilation` (deleting `MarkStoreAsImage`) fix all three worst offenders (emitter
  mutating `DataItem`; the bind pipeline living inside `CSharpEmitter.CallEmitRunUnit`; the public-dictionary
  blackboard). Refinements: (a) the P6 `OoBindCallbacks` seam temporarily re-introduces an emitter→binder callback —
  add a P9 grep-gate to remove it; (b) the settable `DataBinder.OoClasses` handshake is a write channel not in the
  Step-5 sealing list; (c) `WholeGroupReferenced` needs a named `UsageCollectionPass` owner, not the resolver's
  incidental mid-resolve writes; (d) `ProcedureBindPass`/`StorageFormPass` must be *required* in the manifest (no "or
  leave a TODO" escape) so the whole-group→storage-form ordering watermark actually holds. *(CRITIQUE-encapsulation.md)*
- **R6 — Doc-drift + G8 flip under-scopes direct compile-time runtime calls** *(→ PHASE-08, PHASE-15).* `CobolNum`'s
  class doc wrongly describes a native-`long` engine with an "Int128 escape hatch" that does not exist (computation is
  Int128-uniform; `long` is storage-only) — correct it and name the `CobolNum.Store` funnel as the unifying invariant.
  The G8 namespace flip is scoped to emitted `using`s, but the compiler makes **direct** compile-time calls into the
  runtime (`CobolEdit.Format`/`MaskScale` for constant-VALUE folding) that the `RuntimeApi` façade must also cover.
  *(SURVEY-runtime-value.md)*

### Improvement registry (I1–I4)

Owner-proposed refinements (2026-07-18), each anchored to EXISTING infrastructure with **zero architecture impact**
(the bound-tree shape, the edition data, and the conformance machinery already exist). Adopted as below, with the
mechanism refinements noted inline. These turn already-asserted invariants into *checked* gates and publish
already-derivable coverage; none change the pipeline.

- **I1 — Bound-tree backend-neutrality gate** *(→ PHASE-16 M0; a grep leg addable at PHASE-14).* Make R1's neutrality
  claim a CHECKED invariant, not an assertion. **Now (a PHASE-14 grep exit criterion):** a `BackendNeutrality` gate
  over `Binding/Bound/` that fails on the C#-SPECIFIC leak patterns R1 enumerates (`*.CsName`, `*.TablePath`,
  `*.IndexField`, `*.DependCount`, the `*.DynTable` path string — i.e. mangled identifiers / .NET type names / Roslyn
  `Syntax*` / format literals), NOT legitimate COBOL-domain strings (data-names, literal text, PIC). The R1-listed
  existing leaks are the P16 M0 neutralization checklist. **Robust form (PHASE-16, as the backend-neutrality proof):**
  promote the grep to a Roslyn analyzer **COBOLNET9001** (a build error on a NEW leak; a `[BackendNeutral]` assembly
  marker + a `[CSharpArtifact]` opt-out allowlist for the sanctioned residue). NOTE: COBOLNET9001 is an
  analyzer-on-our-own-C# code (the `9xxx` band, free), distinct from the COBOL-facing `COBOLNET0xxx/1xxx` diagnostics.
  ⚠ **Anchor correction:** the seam-proof (`NullBackend`/`DisplayBackend` + the contract test) is **PHASE-16
  Milestone 0** per `DESIGN-backend-abstraction.md`, NOT the roadmap-§3 "PHASE-07 Step 13" — P07 closed at Step 12 and
  the seam-proof did not land there (§3 corrected in the same change set). *(R1; DESIGN-backend-abstraction.md)*
- **I2 — ICE-free robustness gate** *(→ PHASE-14, new exit criterion 8).* Enforce the §1.4 loud-failure invariant
  against MALFORMED input: `CompilerInvocationHarness.TryCompile` over a deterministic **one-token-deletion corpus of
  the committed NIST sources** must never throw an uncaught exception — only diagnostics or a clean success (an
  internal compiler error is a bug, a NAMED diagnostic is the contract). ⚠ **Refinement:** do NOT commit the mutated
  corpus (NIST × per-token ≈ 10^5 files → repo bloat); commit the harness + a fixed seed and GENERATE the deletions at
  test time (reproducible). A sampled subset per push, the full sweep at the P14 gate. Explicitly a bounded
  deterministic corpus, NOT a continuous fuzzer. *(§1.4; §2 principle 1)*
- **I3 — Edition-delta coverage report** *(→ PHASE-14 deliverable; published PHASE-15).* `scripts/gen-edition-delta-
  report.ps1` (a sibling to `gen-vcr.ps1`) folds `constructs.json` + `docs/VERSION_CHANGE_REFERENCE.md` +
  `docs/CONFORMANCE.md` dispositions into `docs/EDITION-DELTA-COVERAGE.md` — Supported / Deferred / Rejected-loud
  tables per 2014→2023 (and prior) delta. Drift-guarded by `EditionDeltaCoverageDriftTest` (the existing generate-then-
  diff pattern the VCR + differential goldens already use). Land the generator at **P14** (all source data exists now;
  it also renders the live P13 wave rollup) and finalize/publish with the **P15** conformance docs.
  *(VERSION_CHANGE_REFERENCE.md, constructs.json, CONFORMANCE.md, gen-vcr.ps1)*
- **I4 — Negative-corpus edition-boundary coverage gate** *(→ PHASE-14, extends exit criterion 4).* EC#4 already
  asserts every DIAGNOSTIC descriptor has ≥1 negative case; I4 adds the orthogonal CONSTRUCT×EDITION axis:
  `NegativeCorpusCoverageTests` enumerates every `constructs.json` row and asserts a below-edition negative fixture
  exists at each earlier-edition boundary the construct crosses (introducedIn 2023 ⇒ 85/2002/2014 witnesses; 2002 ⇒
  85). ⚠ **Mechanism correction:** an `[Ignore("STUB")]` xUnit test is SKIPPED (green) and does NOT block CI — the
  GATE is the coverage assertion (missing fixture ⇒ the test fails red), not ignored stubs; `gen-negative-stubs.ps1`
  only scaffolds the fixture files to author. Scoped against the existing `VersionMatrixTests` (which already
  compile-and-reject per edition from each row's `source`/`expectDiagnostic`): I4 adds the COMPLETENESS check, never
  duplicate reject-tests. *(§2 principle 11; the VCR / version-matrix harness)*

## §8 CONSOLIDATED FORWARD-RESIDUE LEDGER (absorbed from the retired phase docs; every named residue lives HERE now)

- **P10 residues (by name):** per-shape 1510 UDF RETURNING (float/boolean/pointer-class + group shapes) ·
  OPTIONAL formals (0899 `optional-formal`) · the two recursive-WS stages (0899 `recursive-contained-working-
  storage`, `recursive-working-storage-pointer-backed`) · OO class-unit BASED (`OoBasedInClass`) ·
  INITIALIZE-over-pointer-categories · line-seq 06/09/71 + REWRITE + the LINE SEQUENTIAL gate · keyed GR10a FPI
  + keyed ADVANCING emission · cross-run-unit sharing · SORT national-key carry · multiple-LINE repetition (+
  report-OCCURS family) · narrowed-1509 shapes · signed-leaf strong ordering (0899 `strong-group-ordering-
  signed-leaf`) · 1535 `typedef-renames-staged` · 1531 INDEXED-type-≥2× · MAX/MIN-under-explicit-collating.
- **P12 residues:** external-float `E` PICTURE (staged 0899) · FUNCTION-POINTER runtime + restricted
  PROGRAM-POINTER + `ADDRESS OF` spellings (staged 0899) · >>PROPAGATE runtime semantics + §7.3.21.3 SR1
  placement (→ P13 Wave D adjunct) · DYNAMIC LENGTH national FUNCTION LENGTH/BYTE-LENGTH runtime paths (staged
  loud; also inventory row A.4.5-Partial).
- **P13-session residues:** UDF/INVOKE activation boundaries carry EC-EXTERNAL site-mask 0 (no raise there; the
  INVOKE GR7d leg needs the OO activation seam) · Tier-C confined-`byte[]` codec (DESIGN-data-model §2.3;
  deferred, design needs re-basing) · the Wave-I documented follow-ons (SET SIZE SR34 compile check; PERFORM
  UNTIL EXIT SR8 nested-under-VARYING) · everything in the review ledger §24 fix queue (the fix SSOT).
- **P7 pickups still queued:** SymbolTableBuilder-owned storage · route `ReferenceResolver.ResolveUnqualified` +
  the StatementBinder condition lookup through the `SymbolTable` · image-fact caching (O(subtree) perf).

## §9 Verification commands + corpus mechanics (operational reference)

- CLI probe (from a scratchpad dir): `cobol.exe <src.cob> --std 85|2002|2014|2023 -o out.dll --run`.
- Greenfield conformance: `dotnet test tests/Cobol.Net.Tests.Conformance` · unit: `tests/Cobol.Net.Tests.Unit` ·
  characterization: `tests/Cobol.Net.Tests.Characterization` · legacy suite:
  `dotnet test tests/CobolSharp.Tests.Integration --filter FullyQualifiedName~ConformanceTests` ·
  FULL legacy guard: `bash scripts/guard.sh` (fast: `guard-fast.sh`; gate on the VERDICT line).
- Positive corpus: `tests/conformance/{2002,2014,2023}/<name>.cob|.out` + per-dir `manifest.json`
  (enabled ⊕ pending, integrity-asserted). Negative corpus: `tests/conformance/negative/<name>.cob|.err`
  (first line `*> reject-at: <editions>`). Matrix rows: `tests/version-matrix/constructs.json` (+
  `reserved-words.json`); regen `.g.cs` via `scripts/gen-constructs.ps1` / docs via `gen-vcr.ps1`,
  `gen-diagnostics-doc.ps1`. GreenfieldOnly exclusions live in `tests/CobolSharp.Tests.Integration/
  ConformanceTests.cs` — enabling a shared-corpus golden REQUIRES the exclusion or a legacy-suite run SAME
  commit.

## §10 Document map (post-consolidation)

- **THIS DOC** — the only plan. `DEVLOG.md` — the only history (descending). `docs/DOC_INDEX.md` — the doc
  registry (keep in sync).
- **Design SSOTs (unchanged):** `docs/COBOLNET_DESIGN.md` (+ §0.5 deep-dive list) · `docs/rearchitecture/
  DESIGN-*.md` (10) · the subsystem `COBOLNET_*_DESIGN.md` corpus.
- **Evidence ledgers (unchanged):** `docs/rearchitecture/PHASE-13-plan-vs-spec-review.md` (the verified findings
  + §24 fix queue) · `PHASE-11/12-scout-notes.md` (durable spec-to-code traceability) · the two P13 scouts
  (working designs for the remaining waves; DELETE at P13 close) · `docs/CONFORMANCE.md` (the §4.2.16 record) ·
  `docs/VERSION_CHANGE_REFERENCE.md` + `docs/DIAGNOSTICS.md` (generated).
- **Deleted by this consolidation (2026-07-19; content absorbed here):** `resume-prompt.md` → §0/§3;
  `PHASE-00..12-*.md` → Part III records + §8 residues; `PHASE-13-m4-2023-*.md` + `PHASE-13-audit.md` +
  `PHASE-13-scout-notes.json` → §0 worklists + the review ledger (which re-verified the audit);
  `PHASE-14/15/16-*.md` → Part II; `PLAN-bindtime-gating-migration.md` → completed (its ledger absorbed into
  the P03 record). SURVEY-*/CRITIQUE-*/EVAL-* stay as frozen pre-rearchitecture analyses (cited by §7 and the
  review ledger); delete at the P15 doc sweep.


## §11 ANALYSIS BACKLOG (owner-recorded 2026-07-19 — the durable register of USEFUL-BUT-MISSING analyses; every row has a scheduled home)

> These are the known analytical blind spots identified after the P13 plan-vs-spec review reached
> verification-complete. Each row is integrated into the work plan at the named home; when an analysis runs,
> flip its Status here and fold its outputs the same way the review was folded (batched agents, durable
> per-batch commit, adversarial verify on findings). Do NOT silently drop a row — rescheduling is an §0 edit.

   ⛔ **THE ROWS MOVED TO `kb/Work/` (2026-08-04) — THIS SECTION IS THE RATIONALE, NOT THE REGISTER.**
   All 17 analyses are now notes (`kb/Work/A*.md`, `kind: analysis`) alongside the defects, so one query
   answers "what is left" across both. A table here as well was a SIXTH register re-forming — the exact
   thing CLAUDE.md rule 8 forbids, and it appeared because the migration copied the rows without retiring
   the source. Ask the register:

   | to ask | run |
   |---|---|
   | the analysis backlog | `kb/Work.base` → **Analyses (§11)** |
   | everything actionable | `python scripts/spec/work.py next` |

   ⛔ **Do not restore a table here.** If an analysis needs recording, add or edit its `kb/Work/` note.


## §12 RISK REGISTER (top risks to v1.0; review at each phase close)

| # | Risk | Trigger | Response |
|---|---|---|---|
| R-1 | The D17 OO wave overruns (largest single feature block, 4–8 sessions) | wave-local gates slipping past session 8 | Split: land inline-invocation + ACTIVE-CLASS first (smaller), re-tier object-view/parameterized-classes with a D15 review; v1.0 date moves only on owner sign-off |
| R-2 | Step 0a finds underivable per-edition facts | any "underivable" inventory row | D18 pre-authorized acquisition of the historical standards; rows carry documented assumptions in the interim |
| R-3 | A perf surprise in the Tier-B storage model after P15 deletes the comparison | Step 12 cost model flags an order-of-magnitude issue | Step 12 runs BEFORE P15 (gated); escalation is an architecture decision while the legacy comparison exists |
| R-4 | Spend-limit outages mid-campaign | agent-batch failures | MITIGATED: the §3 durable-fold discipline (proven 2026-07-19 — two outages, zero loss); resume via journal caches |
| R-5 | Inventory GAP-count blowout re-opens unbounded scope | Step 0 yields ≫ expected GAP rows | D15 tiering at fold time: only CONFORMANCE-BLOCKING rows gate v1.0; QUALITY/POST-v1.0 scheduled behind it |
| R-6 | **A non-deterministic battery makes "every leg green" unfalsifiable** — observed 2026-07-30: two full Conformance runs on one tree gave 4159/4160 then 4160/4160 | any red that does not reproduce on re-run, or any leg whose count moves without a tree change | ⛔ Never accept "flake" without a NAMED mechanism (`feedback_gate_on_the_verdict_line`). Re-run the named test serially, then the whole leg, and record the distribution. **§11 A12 owns the audit**; until it closes, treat a green battery as necessary-but-not-sufficient evidence for a conformance claim, because the same non-determinism that produced a false RED can mask a real failure |


---

# PART II — LIVE-PHASE EXECUTION DETAIL (absorbed step-by-steps)

# PHASE-14 — Step 0 traceability inventory → matrix closure → greenfield guard → equivalence proof

### Goal

Close the version-correctness program to zero open work and make the greenfield test net self-standing and provably faithful *before* the legacy oracle is severed. Concretely: (1) drive every row of `docs/VERSION_CHANGE_REFERENCE.md` (VCR) to `green`/`GATED` or a written disposition — no `TODO` survives; (2) run the full INV-1 / INV-2 / INV-3 sweeps in both strict and permissive modes, **including golden re-match at `--std 2023` (INV-1-strong at the shipping default edition — the fatal-challenge criterion)**; (3) complete the negative corpus so every registered diagnostic descriptor has ≥1 case, enforced by a registry-coverage unit test; (4) build the **in-repo greenfield census guard** that rebuilds the lost 403/459-census tooling by driving the greenfield `cobol.dll` (run-only + chain-intermediate handling ported from `scripts/guard.sh`); (5) run the **one-time verdict-diff equivalence proof** of the greenfield census guard against the legacy `guard.sh` **while the legacy engine still runs**, and record it; (6) migrate the 11 `LEGACY_DIVERGENT` ISO citations out of `guard.sh` into the greenfield guard and a durable LEDGER doc.

**OUT of scope (P15):** deleting the legacy byte engine / `tests/CobolSharp.Tests.*` / the legacy `guard*.sh` scripts, and the `CobolSharp.* → CobolNet.*` / `CobolNet.Runtime → Cobol.Net.Runtime` namespace flip. P14 *builds and proves* the replacement; P15 *removes* the original.

### Exit criteria (the phase is DONE when all hold)

1. **VCR zero-TODO:** `grep -c '| TODO |' docs/VERSION_CHANGE_REFERENCE.md` returns `0`. Every row is `GATED`/`green` or carries a written disposition (a `DISPOSITION:` note with an ISO citation and the reason it is intentionally not gated). A drift test binds VCR status to harness reality (Step 2).
2. **G7/G8 exit criteria satisfied as counts/exit codes** (the criteria P3 wrote into `docs/COMPLETION_ROADMAP_COUNCIL.md` Phase 1, line 45): continuity-sweep green permissive at all four editions with every strict failure tracing to a recognized edition-band code (0801/0802/0810/0811/0873/0875–0879/0882/0893/0900-band); the 2023-permissive golden run byte-matches; drift tests green over scrubbed metadata.
3. **INV sweeps green:** INV-1 (strict + permissive, weak *and* the strong `--std 2023` golden re-match leg), INV-2 (introduction-gating both directions), INV-3 (behavior-variant rows) all pass in `dotnet test`.
4. **Negative-corpus / registry coverage:** every diagnostic descriptor in the registry (P2) has ≥1 negative-corpus case, asserted by `DiagnosticRegistryCoverageTests`; the corpus manifest drift test is green.
5. **Greenfield guard exits 0** covering: goldens (byte-match ≥357 GREEN), the full census (all 459 census programs compile+run health, golden-less residue accepted by design), per-edition discovery (positive+negative corpora), the INV sweeps, and `dotnet test` (Unit + Conformance + Characterization). Runs cross-platform (`.sh` + `.ps1`).
6. **Equivalence proof recorded** against the still-running oracle: the per-program verdict-diff of `greenfield-guard.sh` vs `guard.sh` is empty except for the 11 documented `LEGACY_DIVERGENT` programs, and the result is committed to `docs/rearchitecture/EQUIVALENCE-PROOF.md`.
7. **`LEGACY_DIVERGENT` citations migrated** into `docs/LEGACY_DIVERGENCE_LEDGER.md` and consumed by the greenfield guard (so nothing is lost when `guard.sh` is deleted in P15).

### STATUS

`NOT STARTED`

> The executing session updates this line to `IN PROGRESS @ step N` after each step, and to `DONE` when all exit criteria hold. Also append a DEVLOG entry per commit boundary (descending, real timestamp) referencing `PHASE-14`.

---

### 2. Rationale — the problems this phase fixes

This phase is the load-bearing hinge between "the compiler works" and "we can safely delete the oracle." The survey/critique findings it closes:

- **The net evaporates at G8 unless a faithful replacement is proven first** (`DESIGN-test-build-ci.md` §1.2, §3.4, §6 risk 4; `COMPLETION_ROADMAP_COUNCIL.md` §4 risk 4). The authoritative NIST regression today is `scripts/guard.sh`, which compiles+runs ~353 programs **through the frozen legacy `cobolsharp.dll`** and diffs `tests/nist/valid/*.txt`. The greenfield NIST coverage is only the 318 golden-bearing rows driven by `NistDifferentialTests.cs` — it does **not** exercise the golden-less census residue (459 census programs − 364 goldens), the run-only programs, or the compile+run health of the full corpus the way `guard.sh` does. When P15 deletes `guard.sh` and the legacy engine, that census coverage is gone unless P14 rebuilds it greenfield **and proves the rebuild matches**, program-by-program, while the oracle is still runnable. Once legacy is deleted the proof is impossible forever (`COMPLETION_ROADMAP_COUNCIL.md` risk 4: "the G8 equivalence window closes unproven").

- **Version-gating is unaudited and the VCR ledger is stale** (`DESIGN-edition-framework.md` P7/P8; `VERSION_CHANGE_REFERENCE.md` — `grep -c TODO` = 117 rows still open, 6 done/GATED as of this writing). The "four compilers in one" mission (`docs/VERSION_TEST_MATRIX_DESIGN.md`) is only validated at its deltas/boundaries; a `TODO` row is an un-proven claim. P3 made the audit harness-driven (`scripts/gen-vcr.ps1` + `--emit-status`); P14 is where the burn-down actually reaches zero and the ledger becomes structurally incapable of drifting.

- **The default shipping edition (2023) is never behaviorally executed before G8** — the critics' one *fatal* challenge (`COMPLETION_ROADMAP_COUNCIL.md` §2 Phase 1, decision #10). `NistDifferentialTests` hard-compiles at `DialectLevel 85` (`CompilerUnderTest.cs`: `CobolNetCompiler(int dialectLevel = 85)`); the INV-1 sweep only asks "does it *compile*" via `check-batch` (`scripts/version-continuity-sweep.sh`), never runs. The `COBOLNET_NIST_STD` / `COBOLNET_NIST_PERMISSIVE` env override (`NistDifferentialTests.cs`) exists precisely so the whole golden run can be re-targeted to `--std 2023 --permissive` and asserted byte-identical; P14 promotes that leg to a hard, always-on G7 exit criterion.

- **Diagnostics are unaddressable without a coverage floor** (`DESIGN-test-build-ci.md` §1.6, §3.5). P2 built the first-class descriptor registry; P14 is where "every rule has a test" becomes an enforced invariant (`DiagnosticRegistryCoverageTests`), so a registered code with no negative-corpus case is a red — closing the false-green class the 0899 catch-all and the `1533`-style code reuse used to hide.

- **The three-way "which programs are green" triplication** (`DESIGN-test-build-ci.md` §1.2, smell #3): `guard.sh` `NIST_TESTS`, the `NistDifferentialTests` golden list, and `tests/nist/chains.tsv`. P0 introduces `tests/nist/corpus.tsv` as the single source; P14's greenfield guard consumes it (not a fourth copy), and the equivalence proof confirms the manifest-driven run reproduces the legacy verdict list before the old sources are deleted (`DESIGN-test-build-ci.md` §6 risk 4 mitigation).

---

### 3. Target end-state (what exists when this phase is DONE)

Files created / changed by P14 (real paths):

**Scripts / tooling**
- `scripts/greenfield-guard.sh` — the in-repo greenfield census guard (bash). Drives `src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll` over the full NIST census (from `tests/nist/corpus.tsv`), compiles+runs every program, diffs golden-bearing ones against `tests/nist/valid/`, accepts golden-less run-only programs (census health), resolves producer→consumer chains, and carries the `LEGACY_DIVERGENT` set as expected-diff sourced from the LEDGER. Exit 0 iff no regression.
- `scripts/greenfield-guard.ps1` — Windows-parity wrapper (thin; may shell the same census-runner logic or a shared `dotnet` entry). Behavior-equivalent output vocabulary.
- `scripts/equivalence-proof.sh` — the one-time verdict-diff harness: runs `guard.sh` and `greenfield-guard.sh`, captures each per-program verdict list, diffs them, and asserts the delta ⊆ `LEGACY_DIVERGENT`. Writes the recorded proof.
- `scripts/gen-vcr.ps1` — (from P3; extended here) regenerates VCR status from `constructs.json` + harness results so `TODO` can never be hand-ticked back in.

**Compiler / CLI**
- `src/Cobol.Net.Cli` — a census *run* mode. Prefer extending the existing `check-batch` command (`Program.cs:103` — parse+bind only, no Roslyn) with a sibling `run-batch` (or `census`) subcommand that compiles **and runs** each manifest entry and reports `MATCH`/`DIFF`/`COMPILE-FAIL`/`RUN-FAIL`/`NO-GOLDEN` per program, so the census guard is one warm process instead of ~459 cold `dotnet` invocations. If a run-batch command is judged too heavy, the guard script may fall back to per-program `cobol` + `dotnet <dll>` (the `guard.sh` shape), but the single-process form is the target (matches P0's Roslyn-cache throughput goal).

**Tests**
- `tests/Cobol.Net.Tests.Conformance/VersionMatrixTests.cs` — (from P0/P3) extended: INV-1 strict+permissive rows over `corpus.tsv`; the `expectDiagnostic` assertions cover every folded-in VCR row.
- `tests/Cobol.Net.Tests.Conformance/VersionBehaviorMatrixTests.cs` — (from P3) INV-3: every `variant`-tagged construct row runs under each `--std` and its stdout is diffed against the per-edition expectation.
- `tests/Cobol.Net.Tests.Conformance/Inv1StrongGoldenTests.cs` — NEW: the always-on INV-1-strong leg — compiles AND runs the full golden set at `--std 2023 --permissive` and asserts byte-identical output (the `COBOLNET_NIST_STD`/`COBOLNET_NIST_PERMISSIVE` path promoted from an env-gated leg to a first-class `[Theory]`).
- `tests/Cobol.Net.Tests.Unit/DiagnosticRegistryCoverageTests.cs` — NEW: asserts every `DiagnosticDescriptor` in the P2 registry is (a) unique-coded and (b) exercised by ≥1 negative-corpus `.err` case (or an inline diagnostic-snapshot case). A registered-but-untested code is a red.
- `tests/conformance/**/manifest.json` + `tests/conformance/negative/*.{cob,err}` — completed so every registered descriptor and every folded-in VCR gate has a witness; `CorpusRunnerTests.Manifest_CoversEveryProgram_NoOverlap` stays green.

**Docs**
- `docs/VERSION_CHANGE_REFERENCE.md` — zero `TODO`; each row `GATED`/`green` or `DISPOSITION:`-noted with citation.
- `docs/LEGACY_DIVERGENCE_LEDGER.md` — NEW (type LEDGER): the 11 `LEGACY_DIVERGENT` programs, each with its ISO §-citation and the reason the golden diverges from the legacy engine's output (migrated verbatim from `guard.sh:122-145`). The greenfield guard reads its divergent-set from here.
- `docs/rearchitecture/EQUIVALENCE-PROOF.md` — NEW: the recorded one-time proof (date, commits of both engines, the verdict-diff output, the ⊆-`LEGACY_DIVERGENT` conclusion). This is the artifact P15 Cut-1 cites as its precondition.
- `docs/DOC_INDEX.md` — rows added for the LEDGER, the equivalence proof, and this phase doc; the VCR row's maintenance note updated ("status generated, never hand-ticked").

**CI (`.github/workflows/build-and-test.yml`)**
- A new `greenfield-guard` job (per-OS or ubuntu) running `scripts/greenfield-guard.sh` (or `.ps1`), now an **authoritative** gate alongside the existing `greenfield-tests` and `inv1-sweep`.
- The existing `guard` (legacy `guard-fast.sh`) job is **kept** and renamed in intent to `legacy-oracle` — the temporary cross-check that the greenfield guard still matches a live legacy run — retained through P14 and deleted in P15.

---

### 4. STEP-BY-STEP

> Ordering rationale: the burn-down and sweeps (Steps 1–5) make the *greenfield truth* complete and green; the guard (Steps 6–8) packages that truth into a self-standing regression; the equivalence proof (Step 9) validates the package against the oracle; the LEDGER migration (Step 10) and CI wiring (Step 11) make it durable. The equivalence proof MUST run while the legacy engine is present — it is the last step that structurally requires the oracle.

#### Precheck (no commit) — reproduce the baseline green

Before touching anything, prove the battery is green from a clean build (incremental builds can mask a regression):

```
dotnet build CobolSharp.sln -c Debug
dotnet build src/Cobol.Net.Cli/Cobol.Net.Cli.csproj -c Debug
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj
bash scripts/guard-fast.sh                 # legacy oracle: ALL GREEN
bash scripts/version-continuity-sweep.sh | tee /tmp/sweep.log ; ! grep -q BREAKS /tmp/sweep.log
```

Expected: conformance + unit green; `guard-fast.sh` prints `=== ALL GREEN ===`; the sweep prints no `BREAKS`. Record the counts in the DEVLOG opener. If any is red, STOP — a precondition phase (P3/P13) is not actually complete.

Confirm the P14 preconditions exist:
- `tests/nist/corpus.tsv` exists (P0). If not present, P0's manifest step was skipped — create it first (fold `NistDifferentialTests` `[InlineData]` ∪ `guard.sh` `NIST_TESTS` ∪ `chains.tsv` into the schema in `DESIGN-test-build-ci.md` §3.2, guarded by `CorpusManifestTests`). This is a P0 deliverable; do it here only if missing.
- The P2 diagnostic-descriptor registry exists (`src/Cobol.Net.Editions/Diagnostics/` with `DiagnosticDescriptor`/`DiagnosticCatalog`). If absent, Step 4's coverage test cannot be authored — escalate; it is a P2 deliverable.

---

##### Step 0 — THE FOUR-EDITION SPEC-TRACEABILITY INVENTORY (the D13 definition-of-done instrument) — NEW, runs FIRST

**Why.** D13 sets the target: 100% CONFORMING per §4.2.16. The P13 plan-vs-spec review proved every existing
coverage instrument is partial (the audit missed mandatory OO surface; capped-sample finders cannot certify
completeness). "All remaining work" is unknowable without a clause-level map. This step builds it.

**The artifact.** `docs/rearchitecture/TRACEABILITY-INVENTORY.md` + `tests/version-matrix/traceability-inventory.json`
(machine-readable; the .md renders from it). One row per construct at CLAUSE/STATEMENT/FUNCTION/DIRECTIVE/FORMAT
granularity — §7.3.x directives · §8 fundamentals sub-clauses (incl. the EC catalog raise surface, identifier
forms §8.4.3.x, operators) · §9 I-O concepts + the §9.1.13 status table · §10–§12 division/paragraph clauses ·
§13.5–13.18 entry types + clauses · §14.9.x statements PER FORMAT · §15.x functions · the §4.2 conformance
obligations + Annex A implementor-documentation items (cross-referenced to CONFORMANCE.md). Rows carry
per-edition applicability flags (85/2002/2014/2023), NOT 4x duplication. SR/GR-level notes only on PARTIAL rows.
Estimated 600–900 rows.

**The four states (+ evidence per row):**
- `LIVE` — implemented; row cites the code seam + the test/golden that proves it.
- `STAGED` — recognized and rejected LOUD with a NAMED diagnostic; row cites the descriptor.
- `DISPOSED` — documented non-claim per CONFORMANCE.md (§4.2.6/§4.2.7/Annex A — conforming); row cites the row.
- `GAP` — anything else (unimplemented+silent, generic parse error, untested claim, wrong behavior).
`UNKNOWN` is a transient load-state, illegal at step exit.

**Method — the PROVEN batch discipline (owner-directed 2026-07-19, validated over 16 review batches):** partition
the spec by clause ranges into ~25–30 batches of ≤10 agents; each agent emits rows (schema-forced) with
evidence; FOLD each batch into the persisted inventory + COMMIT + PUSH before launching the next (durable
against limit outages). Seed from the existing instruments rather than re-deriving: constructs.json + the VCR,
CONFORMANCE.md, the residue ledger (§8 of Part I), the P13 review ledger (its verified findings are pre-made GAP
rows), the conformance/NIST goldens as test evidence. A 2-lens verify pass runs per batch on GAP/PARTIAL rows
only (LIVE rows are spot-sampled, 1 in 10).

**Exit criteria for Step 0:**
1. Zero `UNKNOWN` rows; every row carries evidence.
2. Every `GAP` row has an OWNER: a named wave/phase row in this plan (most land in this phase's Steps 1–11
   worklists or the P13 ledger fix queue) or an explicit CONFORMANCE.md disposition (which converts it to
   `DISPOSED`).
3. A drift guard: a CI test asserts (a) every constructs.json row + CONFORMANCE.md disposition references an
   inventory row id, and (b) every `LIVE` row's cited test still exists. The inventory is thereafter maintained
   in the same change set as any surface change (the CLAUDE.md rule-4 discipline).
4. The DEFINITION OF DONE for the whole mission becomes checkable: 100% conforming = zero GAP rows.

**Step 0a — the per-edition AUTHORITY SUFFICIENCY analysis (§11 row A1; runs alongside the inventory).** For
every pre-2023 applicability flag the inventory assigns, record the DERIVATION AUTHORITY: the 2023 text /
an Annex E backward statement / a NIST fixture / or NONE (underivable). Output = the per-edition sufficiency
verdict; any "underivable" class escalates to the owner as a decision (acquire the historical standard vs a
documented assumption) — it may overturn council decision #1.

**Step 0b — the SR-ENFORCEMENT CENSUS (§11 row A2; deepens the inventory).** For each inventory row, its SRs
get one of: ENFORCED (diagnostic + fixture cited) / LENIENT (dialect-registered leniency) / UNENFORCED (the
gap class the review proved — glued VALUEs, DISPLAY UPON, ASSIGN USING, BWZ/JUST/SIGN). Every UNENFORCED SR
either gets a scheduled fix row or a leniency-registry entry. Feeds the negative corpus, which is the
census regression net.

#### Step 0-W — the OO MANDATORY-SURFACE OPENING WAVE (owner decision D17; runs in a PARALLEL LANE with Step 0)

The four review-confirmed mandatory gaps (ledger §11 batch-4 verdicts V16–V19), implemented as ONE feature
wave per the proven pattern (persisted spec-first scout → supervised implement, CLI-probe each, golden +
below-edition negative per construct → adversarial review): (1) **inline method invocation** — the ISO
§8.4.3.4 IDENTIFIER form `{class|id} :: literal [(args)]` usable in sending positions (+ a disposition for the
shipped non-ISO `inlineMethodInvocationStatement` rule and its dead mis-dated registry gate; ISO intro = 2002,
not 2023); (2) **object-view** §8.4.3.5 (`identifier AS [FACTORY OF] class [ONLY] | interface | UNIVERSAL`) +
the EC-OO-CONFORMANCE raise site; (3) **USAGE OBJECT REFERENCE [FACTORY OF] ACTIVE-CLASS + the ONLY phrase**
§13.18.60.2 (today ACTIVE-CLASS mis-diagnoses as unknown-class); (4) **parameterized classes/interfaces**
(CLASS-ID/INTERFACE-ID USING, REPOSITORY EXPANDS/AS — mandatory 2023 surface). Grammar changes ride the
shared-`.g4` guardrail (ONE full legacy guard for the wave). Sized 4–8 sessions; CONFORMANCE-BLOCKING (D15).

#### THE P14 PARALLEL-LANE MAP (compress wall-clock; the §3 discipline per lane)

- **Lane 1 (analysis):** Step 0 inventory batches + 0a/0b — batched agents, durable per-batch fold.
- **Lane 2 (features):** Step 0-W (the OO wave) — supervised, serial within itself.
- **Lane 3 (infra):** Step 12 perf-suite build + the Step-13 GnuCOBOL fetch/extractor/classification sweep.
- **SERIAL tail (after the lanes):** matrix closure (Steps 1–5 — consumes the inventory) → the guard
  (Steps 6–8, 11) → the equivalence proof (Step 9 — the irreversible-ordering gate) → campaign A (numeric
  depth — consumes inventory rows) → the P15 gates check (§11 A3/A4/A5/A6 complete).
- Lanes share the diagnostic-band pre-allocation rule and NEVER share the EC/gate hot files; integration of
  each lane's output takes the comprehensive gate.


#### Step 1 — Drive the VCR to zero-TODO (green/GATED or written disposition)

**Files:** `docs/VERSION_CHANGE_REFERENCE.md`; per-row, a `constructs.json` row (`tests/version-matrix/constructs.json`) + its `VersionConformancePass` check (the ONE gating mechanism — never a binder-embedded `Check`; see `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`); `tests/conformance/negative/<row>.{cob,err}` for each newly-gated row.

**Change:** enumerate the ~117 `TODO` rows (`grep -n '| TODO |' docs/VERSION_CHANGE_REFERENCE.md`). For each, ONE of:
- **Gate it** (the row names a real, in-scope gating obligation whose feature is implemented by P13): add/confirm the `constructs.json` row + its `VersionConformancePass` check + a negative witness, run the row's matrix cell, then flip the status cell to `GATED (…, DEVLOG NNN)` with the code and site — mirroring the existing `GATED (W2: move-alphanumeric-figurative-removed-2023, 0902 …)` shape already in the file.
- **Disposition it** (the row is intentionally not gated — e.g. an Annex A.4 documented-non-support module per ratified decision #3, a behavior with no observable edition delta, or a spec-undefined choice): replace `TODO` with `DISPOSITION: <one line + ISO § + reason>`. Non-support dispositions must trace to a `COMPLETION_ROADMAP_COUNCIL.md` §5 decision (screen/MCS/commit-rollback/locale/extended-letters/A.4.8/A.4.13/VALIDATE) or the §4.2 conformance document plan.

Work in row-family batches (do NOT try all 117 at once). Natural batches: Table 1 (2014→2023 E.2 substantive), Table 2/3 (new directives / reserved words), Table 5 (behavior rows), the archaic/obsolete flags (VCR 89/90/126/127), the 85→2002 interim rows. For each batch, cite the ISO § and the edition. This is a spec-first burn-down: derive the expected outcome from `specs/ISO_COBOL.md` and cite the § in the row and (for a gate) in the code (`feedback_spec_is_the_oracle`, `project_spec_to_code_traceability`).

**Why:** a `TODO` row is an unproven version-correctness claim; the exit criterion is zero. Making status *derived* (Step 2) is what stops it drifting back.

**Verify (per batch):**
```
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --filter "VersionMatrixTests|EditionGateDiagnosticTests|CorpusRunnerTests"
```
Expected: green, with the new matrix reject/accept cells and negative witnesses passing. At the end of the last batch: `grep -c '| TODO |' docs/VERSION_CHANGE_REFERENCE.md` → `0`.

**COMMIT BOUNDARY** (one per batch): `docs(cobolnet): VCR burn-down batch <name> — <k> rows GATED/dispositioned to zero-TODO (PHASE-14)`. Keep the battery green at each commit.

---

#### Step 2 — Make VCR status harness-derived + add the drift guard

**Files:** `scripts/gen-vcr.ps1` (from P3 — extend); `tests/Cobol.Net.Tests.Unit/VcrStatusDriftTests.cs` (NEW or extend a P3 drift test).

**Change:** ensure `scripts/gen-vcr.ps1` regenerates every row's status column from `constructs.json` + a `VersionMatrixTests --emit-status` run (a row is `GATED` iff its `(construct × edition)` fixture passes; `DISPOSITION` rows are pass-through from a `disposition` field in `constructs.json`). Add `VcrStatusDriftTests` asserting the committed `VERSION_CHANGE_REFERENCE.md` status column equals `gen-vcr.ps1`'s output (mirroring `ConstructRegistryDriftTests`). A future hand-edit that re-introduces a `TODO`, or a gate that silently regresses, now fails the drift test.

**Why:** `DESIGN-edition-framework.md` §2.10 / P8 — "a stale ledger becomes structurally impossible." Without this, Step 1's zero-TODO is a snapshot, not an invariant.

**Verify:**
```
pwsh scripts/gen-vcr.ps1 -Check          # exits non-zero on any drift
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj --filter VcrStatusDriftTests
```
Expected: no drift; test green.

**COMMIT BOUNDARY:** `feat(cobolnet): VCR status is generated from the harness + drift-guarded — no hand-ticked rows (PHASE-14)`.

---

#### Step 3 — Promote the INV-1-strong golden re-match at `--std 2023` to an always-on test

**Files:** `tests/Cobol.Net.Tests.Conformance/Inv1StrongGoldenTests.cs` (NEW); reuses `NistDifferentialTests.RunNist` / `Normalize` / `Chains` (make them `internal static` shared helpers or lift into a `NistRunner` helper class so both test classes call one implementation — `feedback_one_mechanism_per_job`).

**Change:** author a `[Theory]` that, for every golden-bearing program in `corpus.tsv` (status `green`), compiles AND runs it at `DialectLevel: 2023, Permissive: true` and asserts byte-identical normalized output vs `tests/nist/valid/<name>.txt`. This is the `COBOLNET_NIST_STD=2023 COBOLNET_NIST_PERMISSIVE=1` env path (`NistDifferentialTests.cs`) promoted from an env-gated manual leg to a first-class always-run test. Keep the env override too (for ad-hoc runs at 2002/2014), but 2023-permissive now runs unconditionally in `dotnet test`.

If any program diffs at 2023-permissive, it is a real bug in a version-gating behavior (not the test): triage against a VCR behavior row; fix the gate; re-run. This is exactly the fatal-challenge triage (`COMPLETION_ROADMAP_COUNCIL.md` §2 Phase 1: "re-run the 318 goldens at --std 2023 permissive and triage every diff against VCR behavior rows").

**Why:** decision #10 (RATIFIED) — INV-1-strong at the default edition is a G7 exit criterion; the shipping default must be behaviorally executed before G8.

**Verify:**
```
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --filter Inv1StrongGoldenTests
# Sanity that the env path still agrees (optional):
COBOLNET_NIST_STD=2023 COBOLNET_NIST_PERMISSIVE=1 dotnet test ... --filter NistDifferentialTests
```
Expected: all golden programs byte-match at 2023-permissive.

**COMMIT BOUNDARY:** `feat(cobolnet): INV-1-strong — full golden run byte-matches at --std 2023 --permissive, always-on (PHASE-14)`.

---

#### Step 4 — Complete the negative corpus + the registry-coverage floor

**Files:** `tests/Cobol.Net.Tests.Unit/DiagnosticRegistryCoverageTests.cs` (NEW); `tests/conformance/negative/*.{cob,err}` (add missing witnesses); `tests/conformance/negative/manifest.json` (list them); `tests/conformance/{2002,2014,2023}/manifest.json` (enable per-edition positives that P13 landed).

**Change:**
1. Author `DiagnosticRegistryCoverageTests`: enumerate every `DiagnosticDescriptor` in the P2 registry; assert each `Code` is unique; assert each code appears in ≥1 negative-corpus `.err` file (or an inline diagnostic-snapshot case for codes that cannot be triggered by a whole-program `.cob`, e.g. internal ICE guards — those are allowlisted with a reason). The set of `.err`-referenced codes is computed by scanning `tests/conformance/negative/*.err` for the `COBOLNET####` token.
2. For each uncovered code, add a minimal `<name>.cob` that triggers it + a `<name>.err` naming the code, list it in `tests/conformance/negative/manifest.json` with the editions it should reject at, and verify via the CLI before enabling (the discipline `COMPLETION_ROADMAP_COUNCIL.md` W2(c) used: "every (case × edition) rejection AND every pre-removal-edition clean compile verified against the CLI before enablement").

**Why:** exit criterion #4 — "every diagnostic descriptor ≥1 case." Closes the false-green class where a registered code has no test and can silently rot.

**Verify:**
```
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj --filter DiagnosticRegistryCoverageTests
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --filter CorpusRunnerTests
# spot-check a new negative case:
dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll tests/conformance/negative/<new>.cob --std 2023 --check   # expect the .err code
```
Expected: coverage test green (no uncovered descriptor); corpus manifest drift green.

**COMMIT BOUNDARY:** `test(cobolnet): negative-corpus completion + registry-coverage floor (every descriptor has a witness) (PHASE-14)`.

---

#### Step 5 — Run the full INV-1 / INV-2 / INV-3 sweeps (strict + permissive) and close any gap

**Files:** none new necessarily — this is a *run + fix* step over `VersionMatrixTests` (INV-1 weak + INV-2), `Inv1StrongGoldenTests` (INV-1 strong, Step 3), `VersionBehaviorMatrixTests` (INV-3), and `scripts/version-continuity-sweep.sh` (INV-1 permissive, check-batch).

**Change:** run all four; every failure is a version-gating bug to fix (`feedback_spec_is_the_oracle`). Confirm the continuity-sweep exit condition of the G7 criteria: every *strict* later-edition failure traces to a recognized edition-band code (0801/0802/0810/0811/0873/0875–0879/0882/0893/0900-band) — add a sweep post-check that, for each strict `BREAKS`, greps the compile diagnostics for one of those codes and fails otherwise (this makes "traces to a removal/reserved row" machine-checked, not asserted).

**Why:** exit criterion #3 + #2. INV-3 (behavior-variant) is "the currently-weakest leg of four-compilers-in-one" (`DESIGN-edition-framework.md` §2.10) — running every `variant` row under all four `--std` and diffing stdout is the discovery tool that catches un-gated semantic deltas.

**Verify:**
```
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj \
  --filter "VersionMatrixTests|VersionBehaviorMatrixTests|Inv1StrongGoldenTests"
bash scripts/version-continuity-sweep.sh | tee /tmp/sweep.log ; ! grep -q BREAKS /tmp/sweep.log
pwsh scripts/version-continuity-sweep.ps1   # add a .ps1 sibling if P3 didn't (DESIGN-test-build-ci §3.7)
```
Expected: all green; sweep no `BREAKS`; strict breaks (if any) all edition-band-coded.

**COMMIT BOUNDARY:** `test(cobolnet): full INV-1/2/3 sweeps green strict+permissive; strict breaks are edition-band-coded (PHASE-14)`.

---

#### Step 6 — Add the census `run-batch` CLI mode (single warm process)

**Files:** `src/Cobol.Net.Cli/Program.cs` (add a `run-batch` command next to `check-batch` at `:103`); `src/Cobol.Net.Cli/CliOptions.cs` if a DTO is needed.

**Change:** add a `run-batch <manifest>` subcommand. Manifest line format (TSV): `<source>\t<std>\t<name>\t<permissive 0|1>\t<golden|-|run-only>\t<chain-preds space-joined|->`. For each entry it compiles (with NIST X-card preprocessing when the source is under `tests/nist/programs`), runs chain predecessors first (in an isolated dir), runs the program, locates the print file (`<name>.txt`) or stdout (the `guard.sh` discovery order), normalizes (drop CR, strip trailing, mask `COMPUTED=`), and reports one line per program:
`<name>\tMATCH | DIFF | COMPILE-FAIL | RUN-FAIL | NO-GOLDEN | DIVERGENT`. It parallelizes across cores (like `check-batch`) but each program's chain runs serially in its own scratch dir (port the isolation model from `NistDifferentialTests.RunNist` and `guard-fast.sh`). It reuses the cached Roslyn reference set (P0) so 459 compiles are one process.

**Why:** the census guard must exercise compile+**run** health of all 459 programs, not just the 318 golden-bearing ones (`guard.sh` does this today through legacy; the greenfield had no equivalent). A single warm process is the throughput target (P0's Roslyn-cache rationale). Reusing the exact `RunNist`/`guard.sh` normalization and chain logic is what makes the Step-9 equivalence proof clean.

**Verify:**
```
# build a tiny 5-program manifest and run it:
printf 'tests/nist/programs/NC101A.cob\t85\tNC101A\t0\tgolden\t-\n' > /tmp/m.tsv
dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll run-batch /tmp/m.tsv
```
Expected: `NC101A\tMATCH`. Add a run-only (golden-less) program and a chained consumer (e.g. `ST103A` with preds `ST101A ST102A`) and confirm `NO-GOLDEN` and `MATCH` respectively.

**COMMIT BOUNDARY:** `feat(cobolnet): cobol run-batch — warm-process census compile+run over a manifest (PHASE-14)`.

---

#### Step 7 — Build the in-repo greenfield census guard

**Files:** `scripts/greenfield-guard.sh` (NEW); `scripts/greenfield-guard.ps1` (NEW).

**Change:** the guard, driving `cobol.dll` (greenfield) — a faithful greenfield analog of `guard.sh` (Read `scripts/guard.sh` in full; port its census loop, chain handling, print-file/stdout discovery, `normalize()`, the FAIL*/footer `NNN TEST(S) FAILED` pass-fail signal, and the golden-cleanliness sweep). It:
1. Builds `cobol.dll` + copies `Cobol.Net.Runtime.dll` into the run dir.
2. Emits the census manifest from `tests/nist/corpus.tsv` (the single source — `status`, `golden`, `chain-preds` columns), NOT a hand list.
3. Runs `cobol run-batch <manifest>` (Step 6).
4. Aggregates: golden `green` rows must be `MATCH`; `run-only`/golden-less rows must be `COMPILE-FAIL`-free and `RUN-FAIL`-free (census health — output not compared, by design; NIST leaves ~102 programs golden-less); a `DIFF`/`COMPILE-FAIL`/`RUN-FAIL` on a `green` row is a regression → exit 1.
5. Reads the divergent set from `docs/LEGACY_DIVERGENCE_LEDGER.md` (Step 10) — those goldens are ISO-conforming and are reported `DIVERGENT (expected)`, never a regression.
6. Runs the golden-cleanliness sweep (port `guard.sh:232-253`: no empty golden, no `FAIL*` in a golden, no nonzero footer).
7. Prints a census summary `<GREEN>/<census> GREEN` and `=== ALL GREEN ===` / `=== N REGRESSION(S) ===`.

`greenfield-guard.ps1` mirrors it (may call the same `run-batch` + a PowerShell aggregation, or invoke a shared aggregator).

**Why:** exit criterion #5 — the self-standing regression that replaces the legacy `guard.sh` NIST loop and survives P15. Sourcing the census from `corpus.tsv` kills the triplication (smell #3).

**Verify:**
```
bash scripts/greenfield-guard.sh
```
Expected: `=== ALL GREEN ===`, census summary `≥357 GREEN`, exit 0. On Windows: `pwsh scripts/greenfield-guard.ps1` → same verdict. Cross-check the GREEN count equals or exceeds the census criterion (≥357).

**COMMIT BOUNDARY:** `feat(cobolnet): in-repo greenfield census guard (cobol.dll over the full 459-census; run-only + chains) (PHASE-14)`.

---

#### Step 8 — Reconcile the greenfield guard's GREEN set with the golden set

**Files:** possibly `tests/nist/corpus.tsv` (status fixes), `tests/Cobol.Net.Tests.Conformance/NistDifferentialTests.cs` (repoint at `corpus.tsv` `[MemberData]` if P0 didn't).

**Change:** confirm the census guard's `green` set == the `NistDifferentialTests` golden set == the `corpus.tsv` `green` rows == `guard.sh` `NIST_TESTS` minus `LEGACY_DIVERGENT`. Any mismatch is a triplication artifact — fix it in `corpus.tsv` (the single source) and re-point the in-process test's `[MemberData]` at it (if P0 left `[InlineData]` in place, do the fold here). Add/confirm `CorpusManifestTests` asserting: every `tests/nist/programs/*.cob` is listed; every `green` row has a `valid/<name>.txt`; every `divergent` row has a LEDGER citation.

**Why:** the equivalence proof (Step 9) diffs verdict lists; both sides must enumerate the *same universe* from the *same manifest* or the diff is noise (`DESIGN-test-build-ci.md` §6 risk 4).

**Verify:**
```
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --filter "NistDifferentialTests|CorpusManifestTests"
bash scripts/greenfield-guard.sh   # GREEN count unchanged
```
Expected: green; the three green-sets agree.

**COMMIT BOUNDARY:** `refactor(cobolnet): single-source the green NIST set in corpus.tsv; census guard == goldens == matrix (PHASE-14)`.

---

#### Step 9 — The one-time equivalence proof (IRREVERSIBLE ORDERING GATE)

**Files:** `scripts/equivalence-proof.sh` (NEW); `docs/rearchitecture/EQUIVALENCE-PROOF.md` (NEW, the recorded artifact).

**Change:** author `equivalence-proof.sh`:
1. Runs `scripts/guard.sh` (or `guard-fast.sh`) capturing its per-program verdict lines (`NAME: MATCH` / `DIFF` / `LEGACY DIVERGENT` / …) → `legacy-verdicts.txt`.
2. Runs `scripts/greenfield-guard.sh` capturing its per-program verdicts → `greenfield-verdicts.txt`.
3. Normalizes both to `NAME PASS|FAIL|DIVERGENT` (collapse the vocabularies) and diffs them by program name.
4. Asserts the delta ⊆ the 11 `LEGACY_DIVERGENT` programs (where the two engines *legitimately* differ — greenfield is ISO-correct, legacy has the documented hole). Any other disagreement is a proof failure → exit 1.
5. Emits `EQUIVALENCE-PROOF.md` recording: the date, `git rev-parse HEAD`, the legacy engine build id, both full verdict lists, the diff, and the conclusion "greenfield census guard reproduces the legacy verdicts modulo the 11 documented ISO re-baselines — the greenfield guard is a faithful replacement; P15 may proceed."

Run it. Fix any non-divergent disagreement (it is a real bug in either the greenfield guard's discovery/normalization or a genuine census regression) until the delta is exactly `LEGACY_DIVERGENT`.

**Why:** exit criterion #6 and the phase's whole reason for MEDIUM-risk sequencing — this is the **single irreversible ordering constraint**. It can only run while the legacy engine exists (P15 deletes it). `COMPLETION_ROADMAP_COUNCIL.md` §4 risk 4: "deleting the oracle first makes the greenfield guard unverifiable forever." P15 Cut-1 cites `EQUIVALENCE-PROOF.md` as its precondition.

**Verify:**
```
bash scripts/equivalence-proof.sh    # exit 0; writes docs/rearchitecture/EQUIVALENCE-PROOF.md
grep -c '^' docs/rearchitecture/EQUIVALENCE-PROOF.md   # non-empty, recorded
```
Expected: exit 0; the diff is empty modulo the 11 divergent programs; the artifact is written.

**COMMIT BOUNDARY:** `docs(cobolnet): record the one-time greenfield↔legacy equivalence proof — P15 unblock gate (PHASE-14)`. **Do NOT let any P15 legacy-deletion work land before this commit exists and is green.**

---

#### Step 10 — Migrate the LEGACY_DIVERGENT citations into a durable LEDGER

**Files:** `docs/LEGACY_DIVERGENCE_LEDGER.md` (NEW); `scripts/greenfield-guard.sh` / `run-batch` (read the divergent set from the LEDGER, not a hard-coded list); `scripts/guard.sh` (leave as-is — deleted in P15).

**Change:** move the 11-program `LEGACY_DIVERGENT` block and its per-program ISO citations (`guard.sh:122-145` — IX111A §14.9.49.4 GR3a; IX210A/214A/215A §14.9.17/§14.9.41 GR9/§9.1.13.5; NC235A/NC236A §14.9.37.4 GR8b; SQ207M §14.9.46 GR1; ST146A §14.9.30 GR18; SQ101M §14.9.51 GR25a; SQ208M/SQ210M §13.18.34 GR6b) into `LEGACY_DIVERGENCE_LEDGER.md` as a table: `program | ISO § | legacy behavior | ISO-conforming greenfield behavior | golden re-baseline DEVLOG`. Point the greenfield guard's divergent-set loader at this file (a parseable list of program names). Add a `DOC_INDEX.md` row.

**Why:** exit criterion #7 — when P15 deletes `guard.sh`, these 11 citations (the provenance of 11 re-baselined goldens) must not vanish. `COMPLETION_ROADMAP_COUNCIL.md` Phase 8: "migrate the 11 LEGACY_DIVERGENT ISO citations into the new guard / a LEDGER doc."

**Verify:**
```
bash scripts/greenfield-guard.sh    # still ALL GREEN; the 11 reported DIVERGENT (expected) sourced from the LEDGER
```
Expected: identical verdict; the divergent set now flows from the LEDGER.

**COMMIT BOUNDARY:** `docs(cobolnet): LEGACY_DIVERGENCE_LEDGER — migrate the 11 ISO re-baseline citations out of guard.sh (PHASE-14)`.

---

#### Step 11 — Wire the greenfield guard into CI as an authoritative gate

**Files:** `.github/workflows/build-and-test.yml`.

**Change:** add a `greenfield-guard` job (ubuntu; run `bash scripts/greenfield-guard.sh`) as a first-class gate. Add a Windows leg (or a matrix) running `pwsh scripts/greenfield-guard.ps1` to close the OS gap (`DESIGN-test-build-ci.md` §1.5 smell #5 — the NIST regression was Linux-only). **Keep** the existing `guard` (legacy `guard-fast.sh`) job, re-commented as `legacy-oracle` — the temporary cross-check retained through P14/P15-Cut-1 and deleted in P15. Optionally add an `equivalence-proof` job that runs `scripts/equivalence-proof.sh` on a schedule/manually (it needs both engines; it is the pre-P15 insurance).

**Why:** the authoritative regression must run cross-platform and gate every PR before P15; keeping the legacy job is the "cheap insurance through the rearch" the owner chose (decision #3 of `DESIGN-test-build-ci.md` §7, `COMPLETION_ROADMAP_COUNCIL.md` decision #8).

**Verify:** push the branch; confirm the new `greenfield-guard` job (both OSes) is green and `legacy-oracle` still green. Locally: `bash scripts/greenfield-guard.sh` and `pwsh scripts/greenfield-guard.ps1` both exit 0.

**COMMIT BOUNDARY:** `ci(cobolnet): greenfield census guard is authoritative (cross-OS); legacy oracle kept as temporary cross-check (PHASE-14)`.

---

#### Step 12 — PERFORMANCE BENCHMARK + generated-code cost model (§11 row A6 — the perf gate; NEW 2026-07-19)

Build the in-repo benchmark suite (BenchmarkDotNet or a timed harness): compile throughput on large
programs/deep copybooks; runtime cost of the Tier-B string-canonical storage on MOVE-heavy loops;
PC-dispatcher overhead (PERFORM-dense kernels); sequential/indexed file throughput; allocation profile
(strings per MOVE; Int128 paths). Produce the COST MODEL note (what the typed-native model costs where, vs
the design claims) and escalate any architecture-relevant surprise to the owner BEFORE P15 deletes the
legacy comparison. Wire the suite into CI as a trend (not a hard gate). CAMPAIGNS A (numeric depth) and B
(external differential corpus) — §11 rows A3/A4 — also run inside this phase and are P15 preconditions.

#### Step 13 — CAMPAIGN B: retrieve + incorporate the GnuCOBOL testsuite (§11 row A4 — a REQUIREMENT, owner-directed 2026-07-19; P15 gate)

**Goal.** Run the GnuCOBOL project's testsuite (the largest maintained external COBOL test corpus: the
`tests/testsuite.src/*.at` groups — syntax `syn_*.at` + runtime `run_*.at`, ~1000+ groups — plus its NIST
runner configuration) through COBOL.NET, and fold every divergence into the traceability inventory / the
conformance dispositions. External corpora find what every in-house instrument is blind to; this is cheapest
while the legacy oracle still exists.

**⚖ LICENSING POSTURE (load-bearing — do not deviate):** GnuCOBOL and its testsuite are GPL-licensed; this
repo is BSL 1.1. Therefore: **NEVER commit GnuCOBOL test text into this repo.** The committed artifacts are
OURS only: (a) the retrieval script (`scripts/fetch-gnucobol-tests.ps1` — pinned release tarball from the
official GNU mirror, checksum-verified, extracted into the git-ignored `tests/external/gnucobol/`); (b) the
`.at`-format EXTRACTOR (parses autotest groups into (source, expected-stdout | expected-diagnostic) pairs at
run time, in memory or under the ignored dir); (c) the ADAPTER test project (compiles each extracted case with
`cobol.exe` per edition and compares); (d) the CLASSIFICATION + EXPECTATIONS LEDGER
(`tests/external/gnucobol-expectations.json`, independently authored facts: per-case id → ISO-CONFORMING |
GNUCOBOL-EXTENSION (excluded, non-ISO) | IMPLEMENTOR-SPECIFIC (dispositioned) | DIVERGENT (triaged), with our
§-cited rationale). If retrieval or licensing proves blocking, record the
attempt here and escalate the fallback (full-NIST + independently-authored equivalents) as an owner decision.

**WHERE THE LINE SITS (owner decision 2026-07-19 — refines the above):** the prohibition is on the
**substantial expressive content** — their COBOL test SOURCE and their EXPECTED OUTPUT/diagnostic text. Those
are never committed and never reproduced. Short factual **group TITLES and KEYWORDS are citable identification**
— the same nominative use a magazine article or online write-up describing the suite would make — so our
differential report and the expectations ledger MAY carry them, and doing so is what makes the ledger usable for
triage. Case IDs (`syn_move:38`) are coordinates we compute, not their expression. Net effect: **our reports are
committable; their corpus is not.**

**Sub-steps.**
1. The retrieval script + the ignored-dir plumbing (a missing corpus SKIPS the suite with a loud notice —
   never a silent pass; CI may cache the tarball).
2. The `.at` extractor (start with `syn_*.at` diagnostics-only groups, then `run_*.at` stdout groups; skip
   groups needing GnuCOBOL-specific runners).
3. FIRST SWEEP (batched ≤10 agents, durable per-batch fold — the §3 discipline): classify every group into the
   expectations ledger. The GNUCOBOL-EXTENSION class is expected to be large (non-ISO surface — reject-with-
   diagnostic is OUR correct outcome and is so recorded).
4. The adapter runs the ISO-CONFORMING class per edition; every DIVERGENT case gets the review treatment
   (spec-first adjudication, 2-lens verify): our bug → an inventory GAP row / a §24-style fix entry; their
   bug / implementor freedom → a documented disposition in the ledger.
5. CI wiring: a periodic (not hot-path) job running the adapter suite; the expectations ledger is the drift
   guard.

**Exit criteria for Step 13:** the retrieval+extractor+adapter exist and run green on the classified corpus;
the expectations ledger covers 100% of extracted groups (zero UNCLASSIFIED); every DIVERGENT case is
adjudicated (fix row or disposition); the NIST module-coverage map (which CCVS modules our 353 baselines
actually exercise vs the full suite) is produced as part of the same campaign.

### 5. Verification — the full battery at phase end

Run all of the following from a clean build; every one must be green before declaring DONE:

```
# 1. Clean build (no incremental masking)
dotnet build CobolSharp.sln -c Release          # warnings-as-errors on Release
dotnet build src/Cobol.Net.Cli/Cobol.Net.Cli.csproj -c Debug

# 2. In-process greenfield battery
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj
dotnet test tests/Cobol.Net.Tests.Characterization/Cobol.Net.Tests.Characterization.csproj   # gate 2 & 3 (P0)

# 3. INV sweeps
bash scripts/version-continuity-sweep.sh | tee /tmp/sweep.log ; ! grep -q BREAKS /tmp/sweep.log
pwsh scripts/version-continuity-sweep.ps1

# 4. Greenfield census guard (the P15-survival regression) — both OSes
bash  scripts/greenfield-guard.sh          # === ALL GREEN ===, ≥357 GREEN, exit 0
pwsh  scripts/greenfield-guard.ps1

# 5. Legacy oracle still green (needed for the proof)
bash scripts/guard-fast.sh                 # === ALL GREEN ===

# 6. The one-time equivalence proof (records the artifact)
bash scripts/equivalence-proof.sh          # exit 0; delta ⊆ LEGACY_DIVERGENT

# 7. VCR / drift invariants
test "$(grep -c '| TODO |' docs/VERSION_CHANGE_REFERENCE.md)" -eq 0
pwsh scripts/gen-vcr.ps1 -Check
```

**Behavior-neutrality / byte-exact checks:**
- The greenfield census guard's GREEN-golden set is byte-identical to `tests/nist/valid/*.txt` on the NIST acceptance basis (drop CR, strip trailing, mask `COMPUTED=`) — proven per-program by Step 7 and cross-proven against legacy by Step 9.
- INV-1-strong: the full golden set byte-matches at `--std 2023 --permissive` (Step 3), i.e. the shipping default edition produces conforming output on every golden program.
- The equivalence-proof delta is *exactly* the 11 `LEGACY_DIVERGENT` programs — no more, no fewer (a smaller delta means a golden silently regressed to the legacy value; a larger delta means an un-proven census divergence).
- Characterization snapshots (P0 gate 3) are unchanged — P14 adds no emitter behavior, so `Snapshots/` must not move. If a snapshot diffs, a Step-1 gate accidentally changed emission; investigate before re-baselining.

---

### 6. Rollback / resumability

- **Every step is its own commit boundary and leaves the battery green.** To resume after an interruption, read the `STATUS` line, run the Precheck battery, then continue at the next unstarted step. Steps 1–5 are independently valuable and independently revertible (a bad VCR batch or matrix fix reverts without touching the guard).
- **Step 1 (VCR burn-down) is resumable mid-family:** the `grep -c '| TODO |'` count is the progress meter; work in row-family commits so a partial burn-down is still green and mergeable.
- **The guard (Steps 6–8) is additive** — it introduces new scripts/tests and a new CLI subcommand; it changes no existing behavior, so it cannot regress the battery. If `run-batch` proves too complex, the guard may fall back to the per-program `cobol` + `dotnet <dll>` shape (the `guard.sh` idiom) — slower but equivalent; note the fallback in the script header.
- **Step 9 is the hard gate and is idempotent** — re-runnable any number of times while legacy exists; it writes/overwrites `EQUIVALENCE-PROOF.md`. If it fails, DO NOT proceed to P15; the failure is a real census divergence or a guard discovery/normalization bug (most likely: print-file-vs-stdout discovery, chain ordering, or a normalization mismatch — diff a single failing program's two verdict paths by hand).

**Risks + mitigations:**
1. **A VCR row is genuinely ambiguous (2002/2014 edge with no in-repo authority).** Mitigation: per ratified decision #1 (no standards acquisition), disposition it with a provisional-confidence marker and an Annex-E/legacy-inventory citation; a provisional edge is a written disposition, not a `TODO`. Never gate on a guess (`feedback_spec_is_the_oracle`; a wrong gate can reject a valid program).
2. **The equivalence proof reveals a census program the greenfield gets wrong that legacy got right** (a real regression the 318-golden subset never covered). Mitigation: this is the entire point of running the proof *before* deletion — fix the greenfield compiler (`feedback_root_cause_no_workarounds`), never weaken the guard to pass. This may pull in a small feature fix; that is in-scope for P14 (it is a correctness gap the net had been blind to).
3. **`corpus.tsv` chain semantics don't reproduce `chains.tsv` + `guard.sh` ordering** (`DESIGN-test-build-ci.md` §6 risk 4). Mitigation: Step 8 reconciles the three sources before Step 9; the equivalence proof is itself the guard-verify-style diff that confirms fidelity.
4. **Snapshot / warnings-as-errors churn from the new test/CLI code.** Mitigation: the new code is test/tooling, not emitter; keep it warning-clean (Release build gate) and it cannot move a characterization snapshot.

---

### 7. ISO feature work in this phase

P14 is primarily verification/tooling, but the VCR burn-down (Step 1) closes the last version-gating gaps and the INV sweeps (Step 5) may surface un-gated behavior deltas. All work is spec-first; cite `specs/ISO_COBOL.md` §s in every row and gate.

**Spec sections / editions in play (Step 1 batches + Step 5 discoveries):**
- **2014→2023, Annex E.2 substantive changes** (VCR Table 1, ~50 rows): the remaining `TODO` removal/behavior rows — CALL … ON OVERFLOW removal (E.2 item 1c, §14.9.11), ALIGN/strong-typing bit alignment (E.2 item 2), boolean shift operators B-SHIFT-* (E.2 item 3, §8.8.x — new-feature-gate ≥2023), UCS user-defined-word character changes (E.2 item 4), the nine new directive words (E.2 item 5). Each: positive at ≥ intro, negative (reject with the edition-band code) at intro−1; behavior rows diff stdout per `--std` (INV-3).
- **2023 new reserved words** (VCR Table row 32; ISO §8.9): the interval-encoded witnesses already exist — confirm each has a matrix row + negative case (Step 4).
- **Archaic / obsolete flags** (VCR 89/90/126/127; ISO §4.2.12 archaic / §4.2.13 obsolete): EXIT PROGRAM, NEXT SENTENCE — 0903 warnings at ≥2023, their own sub-code in the band (`COMPLETION_ROADMAP_COUNCIL.md` D12).
- **85→2002 / 2002→2014 interim rows** (VCR Table 7): where the 2023 spec cannot adjudicate, disposition with a provisional edge + legacy-inventory citation (ratified decision #1).
- **Annex A.4 documented-non-support dispositions** (screen A.4.2, MCS A.3, commit/rollback A.4.3, locale A.4.9, extended-letters A.4.6, A.4.8, A.4.13, VALIDATE A.4.14 — per ratified decision #3): these VCR rows are `DISPOSITION:` (documented non-support with a uniform diagnostic + the §4.2 conformance-document plan), never `TODO`.

**Conformance tests / goldens to add:**
- One negative-corpus `.cob`/`.err` per newly-gated VCR row (Step 1) + per uncovered diagnostic descriptor (Step 4), listed in `tests/conformance/negative/manifest.json`, verified against the CLI at each named edition before enablement.
- INV-3 behavior-variant `.out` fixtures for every `variant`-tagged row (Step 5), one per edition where the behavior differs.
- The INV-1-strong leg (Step 3) adds no new goldens — it re-runs the existing `tests/nist/valid/*.txt` at `--std 2023 --permissive`.

No new *positive* NIST goldens are minted in P14 (the corpus is COBOL-85; new positive per-edition corpora are P9–P13 work). P14 proves what exists is correct and complete, and packages it into a self-standing, equivalence-proven guard.

# PHASE-15 — G8 legacy retirement (three cuts + CUT 2.5 D10) + conformance docs + namespace flip

### GOAL (one paragraph)
Sever the frozen legacy byte-engine oracle from the build and test graph, delete `src/CobolSharp.*` entirely, flip
the runtime `RootNamespace` from `CobolNet.Runtime` to `Cobol.Net.Runtime` (routed through the single `RuntimeApi`
façade so the emitted `using` changes in exactly one place), and publish the complete ISO/IEC 1989:2023 §4.2.16 user
/ conformance documentation set (the implementor-defined element list A.1, the processor-dependent claims-and-absences
A.3, the optional-element claims A.4 including the A.4.10/11/12 rows, nonstandard extensions and any added reserved
words §4.2.10, archaic §4.2.12 and obsolete §4.2.13 identification, and the §4.2.3/§4.2.4 interaction statements). All
deletions here are irreversible and are gated on the Phase-14 equivalence proof having been green; the legacy source
is preserved at an annotated git tag with a WSL reproduction recipe before it is deleted.

> **⛔ SUB-TRACK — D10 (SUBSCRIPT-mode removal).** The owner's
> D10 ruling (master §6 D10) — FULLY REMOVE the lexer `SUBSCRIPT` mode + the binder subscript re-parse, replacing the
> flat `SUB_*` stream + the ~250-line hand-rolled C# re-parsers with interpreted grammar rules — could not land inside
> PHASE 04's byte-neutral window: the FROZEN legacy compiler consumes `SUB_*`/`SubscriptEntryContext` (`ExpressionBinder.
> BindSubscriptEntry`), so the machinery cannot leave the SHARED grammar until the legacy tree is deleted. That deletion
> is **PHASE 15 Cut 2** — so PHASE 15, immediately AFTER Cut 2, is the first place D10 is realistically doable. It runs as
> the isolated **§"CUT 2.5"** sub-track (after the legacy delete, before the Cut-3 namespace flip). The decision-complete
> DESIGN is `DESIGN-frontend-grammar.md §9` (incl. the §9.4 ISO §8.3.5 space-separator decision the executing session must
> resolve first). This is a rearchitecture task riding the cleanup phase because that is where its blocker clears.

### EXIT CRITERIA (phase is DONE when ALL hold)
0. **(D10 sub-track) The SUBSCRIPT lexer mode + the flat `SUB_*` stream + the hand-rolled C# subscript re-parsers are
   REMOVED**, replaced by interpreted grammar rules per `DESIGN-frontend-grammar.md §9` (retaining only the minimal
   spec-compelled WS mechanism per §9.4); the greenfield battery + `guard.ps1` stay green; a subscript/ref-mod/space-
   separated-args/nested-FUNCTION corpus (the §9.5 D10.1 set) is green. (Sequenced after Cut 2; see §"CUT 2.5".)
1. **Grep-clean of legacy references:** no `src/CobolSharp.*` project, no `ProjectReference` to a `CobolSharp.*`
   project anywhere, no `CobolSharp.Compiler`/`CobolSharp.Runtime` type reference in any `src/Cobol.Net.*` or
   `tests/Cobol.Net.*` file, no `CobolSharp.sln` entry for a legacy project, and no `scripts/guard*.sh` /
   `compliance.sh` / `nist-batch.sh` / `run-suite.sh` in the tree.
2. **One greenfield guard exits 0:** the greenfield-only battery (below) is green from a clean checkout on Windows and
   Linux; a single authoritative guard command (`scripts/guard.ps1`, cross-platform) returns exit 0.
3. **The §4.2.16 conformance document is published:** `docs/CONFORMANCE.md` exists, is complete per the section map in
   §7 of this doc, is linked from `README.md` and `docs/DOC_INDEX.md`, and every claim cites a spec § and (where a
   behavioral claim) a passing conformance test / golden.
4a. **Static runtime facades RETIRED with the flip (owner-ratified 2026-07-16 — DESIGN-runtime-library §6 Q3):**
   the P8 delegating facades (`ProgramRegistry`/`ExceptionState`/`ExternalStore`/`CobolModule`/
   `ExternalSwitches`/`CobolFile`/`AcceptSource` statics) exist only as the pre-G8 byte-stability scaffold and are
   DELETED at Cut 3; emitted code reaches the instances through the run-unit driver's ONE captured
   `RunUnit` local (capture-once — never a per-statement `AsyncLocal` read on hot paths), routed via the
   `RuntimeApi` façade so the retirement + namespace flip is one emitted-surface change and ONE reviewed
   characterization re-baseline.
4. **Runtime namespace flipped with emitted code green:** `RootNamespace` is `Cobol.Net.Runtime`; the `Generated/`
   regenerates clean; a representative program compiles, its `.g.cs` shows the new `using`, and it runs byte-identically
   to before the flip.
5. **DOC_INDEX reconciled:** `docs/DOC_INDEX.md` has no rows pointing at deleted docs/scripts, has rows for
   `CONFORMANCE.md` and this phase doc, and the count/preamble is updated.

### STATUS
`NOT STARTED`
<!-- The executing session updates this line to `IN PROGRESS @ step N` and finally `DONE`.
     Keep the per-step checkboxes in §4 current so an interrupted session can resume exactly. -->

---

### 1. Preconditions to VERIFY before starting (do not skip)

> **⛔ §11 ANALYSIS GATES (added 2026-07-19):** P15 shall NOT begin the oracle deletion until §11 rows A3
> (numeric depth), A4 (external differential corpus), A5 (runtime isolation census — with the §24 tier-7 fix
> unit), and A6 (the P14 Step-12 perf gate) are COMPLETE — each is cheap only while the legacy comparison
> exists. A8 (the interpretations register) executes INSIDE this phase's conformance-documentation step.
A future session must confirm the world is in the expected shape, because P15 is destructive. Run these and abort if
any fails — the missing precondition belongs to an earlier phase, not here.

```bash
# P14 done: the greenfield guard + equivalence proof exist and are green.
ls tests/nist/corpus.tsv                                  # P0 corpus manifest must exist
ls scripts/guard.ps1                                      # P0/P14 cross-platform greenfield guard must exist
ls tests/differential/**/*.out 2>/dev/null | head         # P0/R1 baked goldens must exist (severs the oracle)
# P1 done: the greenfield tree is already renamed off the legacy namespace.
grep -rl "namespace CobolSharp.Compiler" src/Cobol.Net.* ; echo "expect: NO hits"
# P7/P8 done: the RuntimeApi façade is the single runtime ABI surface.
ls src/Cobol.Net.Compiler/CodeGen/**/RuntimeApi.cs 2>/dev/null || find src/Cobol.Net.Compiler -name RuntimeApi.cs
```

Expected: `corpus.tsv`, `guard.ps1`, the baked `*.out` goldens, and `RuntimeApi.cs` all exist; the greenfield tree has
**zero** `namespace CobolSharp.Compiler` declarations (P1 renamed them all). If `RuntimeApi.cs` does not exist, step 9
(the namespace flip) cannot be done as a one-file change — STOP and finish P7/P8 first.

**Baseline the battery once, green, before any change** (this is the number every later step must reproduce):

```bash
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj
pwsh scripts/guard.ps1        # greenfield authoritative guard (P0/P14); exit 0
```
Record the pass counts (conformance ~2028+, unit ~213+; NIST corpus all green). Any later step that changes these
counts is a regression to investigate before the commit boundary.

---

### 2. Rationale — the problems this phase fixes
The AS-IS dossier and the sibling DESIGN docs identify the load-bearing weaknesses this phase closes:

1. **The differential net is coupled to code that gets deleted.** `tests/Cobol.Net.Tests.Conformance` still
   `ProjectReference`s the legacy engine (`Cobol.Net.Tests.Conformance.csproj:34-35` → `CobolSharp.Compiler.csproj`
   + `CobolSharp.Runtime.csproj`), and `CompilerUnderTest.cs` opens with
   `using LegacyCompilation = CobolSharp.Compiler.Compilation; using LegacyState = CobolSharp.Runtime.ProgramState;`
   plus a `LegacyCompiler : ICompilerUnderTest`. ~60 `*DifferentialTests.cs` assert `cobolnet == legacy` at runtime.
   DESIGN-test-build-ci §"Risks" #1: *"The net evaporates at G8."* P0/R1 already **baked** the legacy stdout into
   committed goldens (`tests/differential/**/*.out`) and rewrote those tests to golden comparison, so by P15 the net
   is self-standing — this phase only **removes the now-dead legacy edge**.
2. **The CI authoritative gate is a Linux-only bash loop over the frozen engine.** `.github/workflows/build-and-test.yml`
   job `guard` runs `scripts/guard-fast.sh`, which builds `src/CobolSharp.CLI` and runs the legacy `~350` NIST loop.
   That is the *legacy* gate; the greenfield gate is a separate job. Post-bake it is pure redundant insurance and must
   be retired so the greenfield in-process NIST run (P0's `corpus.tsv`-driven `NistDifferentialTests`) becomes the
   whole regression, identical on both OSes (DESIGN-test-build-ci §3.8, closes frontend smell "NIST is Linux-only").
3. **Seven guard scripts and two legacy test projects exist only to run/parallelize the legacy NIST loop.**
   `scripts/{guard.sh,guard-fast.sh,guard-run-group.sh,guard-verify.sh,compliance.sh,nist-batch.sh,run-suite.sh}` and
   `tests/CobolSharp.Tests.{Unit,Integration}` are all legacy-only (DESIGN-test-build-ci §4, "delete (G8)"). They are
   dead once NIST runs in-process.
4. **The runtime carries a deferred rename.** `src/Cobol.Net.Runtime/Cobol.Net.Runtime.csproj` has
   `AssemblyName=Cobol.Net.Runtime` but `RootNamespace=CobolNet.Runtime`, and every runtime file declares
   `namespace CobolNet.Runtime;` — an assembly/namespace incoherence deliberately deferred to G8 to keep the emitted
   `using CobolNet.Runtime;` byte-stable through G0–G7 (csproj banner; DESIGN-runtime-library §2.8). With the
   `RuntimeApi` façade now owning the emitted `using`s, the flip is a one-file emitted change.
5. **No published conformance documentation.** ISO §4.2.16 *requires* an implementation to document its
   implementor-defined (§4.2.5 / A.1), processor-dependent (§4.2.6 / A.3), optional (§4.2.7 / A.4), nonstandard
   (§4.2.10), archaic (§4.2.12 / F.1) and obsolete (§4.2.13 / F.2) elements and its non-COBOL / cross-implementation
   interaction (§4.2.3 / §4.2.4). A "commercial-quality, full-ISO compiler" (the North Star) cannot *claim* conformance
   without this artifact. It is authored here because only now (post P9–P13) is the feature set frozen enough to be
   accurately documented.

---

### 3. Target end-state (concrete — what exists when P15 is DONE)
Files/dirs **deleted:**
- `src/CobolSharp.Compiler/`, `src/CobolSharp.Runtime/`, `src/CobolSharp.CLI/` (the entire byte engine + legacy CLI).
- `tests/CobolSharp.Tests.Unit/`, `tests/CobolSharp.Tests.Integration/`.
- `scripts/guard.sh`, `scripts/guard-fast.sh`, `scripts/guard-run-group.sh`, `scripts/guard-verify.sh`,
  `scripts/compliance.sh`, `scripts/nist-batch.sh`, `scripts/run-suite.sh`.
- `tests/Cobol.Net.Tests.Conformance/` legacy pieces: the `LegacyCompiler` class + `ICompilerUnderTest.Legacy*` and the
  two `ProjectReference`s to `CobolSharp.*` (`CompilerUnderTest.cs`, the `.csproj`).
- The `legacy-oracle` (currently named `guard`) CI job in `.github/workflows/build-and-test.yml`.
- Any `InternalsVisibleTo Include="CobolSharp.Tests.Unit"` in greenfield csprojs (e.g.
  `Cobol.Net.Frontend.csproj:18`).

Files/dirs **changed:**
- `src/Cobol.Net.Runtime/Cobol.Net.Runtime.csproj`: `RootNamespace` → `Cobol.Net.Runtime`; csproj banner updated
  (no longer "renamed at G8; stays CobolNet.Runtime through G0-G7").
- Every `src/Cobol.Net.Runtime/**/*.cs`: `namespace CobolNet.Runtime[.X]` → `namespace Cobol.Net.Runtime[.X]`.
- The `RuntimeApi` façade (`src/Cobol.Net.Compiler/CodeGen/.../RuntimeApi.cs`): the ONE place the emitted `using`
  namespace(s) is produced flips to `Cobol.Net.Runtime`. Every runtime member reference in generated code follows.
- `CobolSharp.sln`: legacy project entries removed; solution builds only the `Cobol.Net.*` projects + the two
  greenfield test projects.
- `.github/workflows/build-and-test.yml`: reduced to an OS-matrix `build-test` job (build `-warnaserror`; conformance +
  unit `--no-build`) + the `version-sweep` (INV-1) job. No legacy job.
- `docs/DOC_INDEX.md`: rows for deleted docs/scripts removed; rows for `CONFORMANCE.md` and this phase doc added.

Files/dirs **created:**
- `docs/CONFORMANCE.md` — the complete ISO §4.2.16 conformance / user-documentation set.
- A git **annotated tag** `legacy-byte-engine-final` at the commit just before Cut 2, with the WSL run recipe in its
  tag message (and a short `docs/rearchitecture/LEGACY-ARCHIVE.md` note recording the tag + recipe for discoverability).

Invariants still upheld: typed-native data only; the full greenfield battery green; four-editions-in-one; JSON/XML
absent.

---

### 4. STEP-BY-STEP (numbered, ordered, resumable)
> Convention: each step is a small, independently-green change. `[ ]`/`[x]` checkboxes track resumability — an
> interrupted session reads the last `[x]` and the STATUS line and resumes at the next `[ ]`. Commit boundaries are
> called out; keep the battery green at each. Ordering is deliberate: **Cut 1 (test graph) → Cut 2 (delete engine) →
> Cut 3 (namespace flip) → conformance doc**, so the destructive engine delete happens only after the test graph no
> longer references it, and the namespace flip happens against a tree that no longer builds the legacy stack.

#### CUT 1 — Drop legacy from the build & test graph
The engine files still exist on disk after Cut 1; only their edges into the greenfield build/test/CI graph are cut.

- [ ] **Step 1 — Sever the conformance project's legacy `ProjectReference`s and delete `LegacyCompiler`.**
  - Files: `tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj` (remove lines 34-35, the
    `CobolSharp.Compiler`/`CobolSharp.Runtime` `ProjectReference`s); `tests/Cobol.Net.Tests.Conformance/CompilerUnderTest.cs`
    (delete `using LegacyCompilation = …`, `using LegacyState = …`, the `LegacyCompiler` class, and the `Legacy*`
    members/branches of `ICompilerUnderTest`; keep `CobolNetCompiler` + `CutRunner`). Any remaining `DifferentialHarness`
    smoke that constructs `LegacyCompiler` is repointed to golden comparison or removed.
  - Why: this `ProjectReference` pair is the ONLY compile-time dependency of the greenfield tree on the legacy engine
    (verified: `src/Cobol.Net.*` csprojs reference only `Cobol.Net.*` + Antlr). P0/R1 already baked the goldens and
    rewrote the differential tests to `AssertMatchesGolden`, so nothing behavioral is lost.
  - Verify:
    ```bash
    grep -rn "CobolSharp" tests/Cobol.Net.Tests.Conformance/ ; echo "expect: NO hits"
    dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj
    ```
    Expected: no `CobolSharp` hits; conformance battery pass count == the §1 baseline (the ~60 differential tests now
    read committed `*.out` goldens, not a live legacy run).
  - **COMMIT BOUNDARY.** Suggested message:
    `refactor(cobolnet): P15 Cut 1a — sever conformance project's legacy oracle ProjectReference (goldens are self-standing)`

- [ ] **Step 2 — Point CI's authoritative gate at the greenfield guard; delete the legacy `guard` job.**
  - File: `.github/workflows/build-and-test.yml`. Delete the `guard` job (runs `scripts/guard-fast.sh` over the legacy
    engine). Confirm `greenfield-tests` (conformance+unit) and `inv1-sweep` remain and are green. Collapse
    `windows-build-test` into an OS-matrix `build-test` per DESIGN-test-build-ci §3.8 (matrix `[ubuntu-latest,
    windows-latest]`; `dotnet build CobolSharp.sln -warnaserror` — but see step 6, the sln still contains legacy at
    this point, so for THIS step keep building the greenfield test projects explicitly; the sln slim-down in step 6
    lets a later edit switch to a whole-sln build). Remove `InternalsVisibleTo` for `CobolSharp.Tests.Unit` from
    greenfield csprojs so nothing references the legacy test assembly name.
  - Why: post-bake the legacy job is redundant insurance; the greenfield in-process NIST run is the whole regression
    (DESIGN-test-build-ci §3.8: post-G8 `build-test` becomes the whole gate, identical on both OSes).
  - Verify: push to a branch and confirm the workflow runs only the greenfield jobs and is green; locally
    `pwsh scripts/guard.ps1` exits 0.
  - **COMMIT BOUNDARY.** Suggested message:
    `ci(cobolnet): P15 Cut 1b — retire the legacy guard-fast.sh job; greenfield in-process NIST is the authoritative gate`

- [ ] **Step 3 — Sever `tools/DifferentialBakeTool`'s legacy dependency.**
  - File: `tools/DifferentialBakeTool` (created in P0). The bake is a one-time operation that has already run; the tool
    must no longer reference `CobolSharp.*`. Either (a) delete the tool outright (the goldens are committed and the
    bake never needs re-running against legacy — recommended), or (b) if kept as a re-bake maintenance utility, strip
    the `CobolSharp.*` `ProjectReference` and gate the legacy code path out. Prefer (a).
  - Why: DESIGN-test-build-ci §"Cut 1 … sever tools/DifferentialBakeTool's legacy dependency". A dangling legacy
    reference here would block Cut 2.
  - Verify: `grep -rn "CobolSharp" tools/ ; echo "expect: NO hits"`; `dotnet build CobolSharp.sln` still succeeds
    (legacy projects still present but now referenced only by themselves).
  - **COMMIT BOUNDARY.** Suggested message: `chore(cobolnet): P15 Cut 1c — remove DifferentialBakeTool legacy oracle dependency`

- [ ] **Step 4 — Delete the legacy guard scripts.**
  - Files: `scripts/guard.sh`, `scripts/guard-fast.sh`, `scripts/guard-run-group.sh`, `scripts/guard-verify.sh`,
    `scripts/compliance.sh`, `scripts/nist-batch.sh`, `scripts/run-suite.sh`. KEEP `scripts/guard.ps1` (greenfield
    authoritative), `scripts/version-continuity-sweep.sh` (INV-1, greenfield CLI), `scripts/gen-reserved-words.ps1`
    (codegen), and any P4 `gen-cobol-words.ps1` / P3 `gen-vcr.ps1`.
  - Why: every deleted script exists solely to run or parallelize the legacy NIST loop or legacy dashboards
    (DESIGN-test-build-ci §4). `version-continuity-sweep.sh` drives the **greenfield** `cobol check-batch`, so it stays.
  - Verify:
    ```bash
    ls scripts/  # confirm only the kept scripts remain
    grep -rn "guard.sh\|guard-fast\|compliance.sh\|nist-batch\|run-suite" .github/ docs/ scripts/ ; echo "expect: NO live references"
    ```
  - **COMMIT BOUNDARY.** Suggested message: `chore(cobolnet): P15 Cut 1d — delete the 7 legacy guard/NIST bash scripts (NIST now runs in-process)`

#### CUT 2 — Delete the byte engine
At the start of Cut 2, NOTHING in the greenfield build/test/CI graph references `CobolSharp.*` (Cut 1 proved it). The
only remaining references are the legacy projects referencing each other, and the two legacy test projects.

- [ ] **Step 5 — Tag & archive the legacy engine BEFORE deleting it.**
  - Create an annotated tag at HEAD (which still contains the engine) with a WSL reproduction recipe in the message, so
    the frozen oracle is recoverable forever:
    ```bash
    git tag -a legacy-byte-engine-final -m "Final commit containing the frozen CobolSharp.* byte-engine oracle (pre-G8 Cut 2).
    To run the legacy engine for a differential spot-check: check out this tag, then on WSL/Linux:
      dotnet build src/CobolSharp.CLI/CobolSharp.CLI.csproj
      bash scripts/guard.sh   # (this tag still has the guard scripts)
    The greenfield goldens under tests/differential/**/*.out and tests/nist/valid/*.txt were baked from this engine."
    git push origin legacy-byte-engine-final
    ```
  - Also write `docs/rearchitecture/LEGACY-ARCHIVE.md` (short): the tag name, the recipe, and the note that the engine
    is intentionally absent from `main` post-P15. Add a `DOC_INDEX.md` row for it.
  - Why: DESIGN scope: *"preserved at a git tag with a WSL run recipe"*. Deletion is irreversible; the tag is the
    rollback.
  - Verify: `git tag -l legacy-byte-engine-final` lists it; `git show legacy-byte-engine-final --stat | head` shows the
    engine present at the tag.
  - **COMMIT BOUNDARY.** Suggested message: `docs(cobolnet): P15 Cut 2a — archive the legacy byte engine at tag legacy-byte-engine-final + WSL recipe`

- [ ] **Step 6 — Remove legacy projects from the solution and delete the legacy trees.**
  - Files: remove from `CobolSharp.sln` the entries for `src\CobolSharp.Compiler`, `src\CobolSharp.Runtime`,
    `src\CobolSharp.CLI`, `tests\CobolSharp.Tests.Unit`, `tests\CobolSharp.Tests.Integration` (use
    `dotnet sln CobolSharp.sln remove <path>` for each). Then delete the directories:
    `src/CobolSharp.Compiler/`, `src/CobolSharp.Runtime/`, `src/CobolSharp.CLI/`,
    `tests/CobolSharp.Tests.Unit/`, `tests/CobolSharp.Tests.Integration/`.
  - Note the solution FILE is named `CobolSharp.sln`. Renaming the solution file to `Cobol.Net.sln` is a nicety but is
    a wider ripple (CI + docs reference it). Recommendation: **keep the filename `CobolSharp.sln` in P15** to avoid
    churn, and record the optional rename as a follow-on in the post-G8 architectural review (owner decision 11, out of
    scope here). If renamed, do it as its own step + `git mv` and update every `CobolSharp.sln` reference in CI/docs.
  - Why: DESIGN-module-topology §10 / COBOLNET_DESIGN §16 G8. Cut 2 is the actual removal of the byte substrate the
    PIVOT mandated never to fall back to.
  - Verify:
    ```bash
    ls src/ ; echo "expect: only Cobol.Net.* dirs"
    grep -rn "CobolSharp\.\(Compiler\|Runtime\|CLI\)" --include=*.csproj --include=*.sln --include=*.cs \
        src tests tools ; echo "expect: NO hits"
    dotnet build CobolSharp.sln            # builds only Cobol.Net.* now
    dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj
    dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj
    pwsh scripts/guard.ps1                 # exit 0
    ```
    Expected: build succeeds with no legacy project; grep clean; battery pass counts == §1 baseline.
  - **COMMIT BOUNDARY.** Suggested message:
    `feat(cobolnet)!: P15 Cut 2b — DELETE the src/CobolSharp.* byte engine + legacy test suites (G8; archived at tag)`

- [ ] **Step 7 — Grep-clean sweep for legacy residue.**
  - Search the whole repo (excluding `bin/`, `obj/`, `.git/`) for stale references and fix each: doc comments naming
    "the legacy CobolSharp.Compiler assembly" (notably `src/Cobol.Net.Frontend/Pipeline/Frontend.cs:16` banner —
    correct it to state the frontend is self-contained; P4/P1 own the code rename but the banner text may still be
    stale), this plan's §0 / `CLAUDE.md` "legacy oracle" live-state mentions, and any `docs/` reference to the
    deleted scripts.
  - Verify:
    ```bash
    grep -rn "CobolSharp" --include=*.cs --include=*.csproj --include=*.md --include=*.yml \
        --include=*.ps1 --include=*.sh . | grep -v "legacy-byte-engine-final\|LEGACY-ARCHIVE\|CobolSharp.sln"
    ```
    Expected: the only surviving `CobolSharp` mentions are the intentional archive references (tag name, `LEGACY-ARCHIVE.md`,
    the `CobolSharp.sln` filename if kept) and historical DEVLOG entries (DEVLOG is an append-only ledger — do NOT
    rewrite history; leaving past entries is correct).
  - **COMMIT BOUNDARY** (if any fixes were needed). Suggested message:
    `docs(cobolnet): P15 Cut 2c — scrub stale legacy-oracle references from banners/live docs`

#### CUT 2.5 — D10: SUBSCRIPT-mode removal (the owner-override sub-track)
Sequenced HERE, after Cut 2, because Cut 2 deleted `src/CobolSharp.*` — so `SUB_*`/`CobolParserCore.SubscriptEntryContext`
are no longer consumed by any legacy code and the SUBSCRIPT machinery can finally leave the SHARED grammar. This is a
HIGH-risk rearchitecture task; keep it a self-contained sub-track that does not touch the Cut-1/2/3 cleanup work.
Execute it FROM `docs/rearchitecture/DESIGN-frontend-grammar.md §9` (the decision-complete design), staged §9.5
D10.1–D10.5, battery-green at every commit boundary.

- [ ] **Step D10.1 — resolve §9.4 + land the before-corpus.** Answer the §9.4 space-separator decision (recommended
  Option A — preserve ISO §8.3.5 space-separated subscript/argument lists via a scoped WS mechanism; Option B narrows the
  language and is a spec violation). Add the NEW conformance/characterization corpus (§9.5 D10.1: multi-subscript
  space/comma lists, relative offsets `I+1` vs `I + 1`, signed literals `+1`/`-15.6`/`-.5`, ref-mod `(a:b)`/`(a:)`,
  qualified subscripts, nested FUNCTION args, string/national/boolean args, `table(ALL)`) captured GREEN first. **COMMIT.**
- [ ] **Step D10.2 — converge ref-mod** onto the DEFAULT-mode `refModPart`; delete the ref-mod branch of the binder's
  `InterpretSubscripts`. **COMMIT.**
- [ ] **Step D10.3 — interpreted subscript grammar rule** (per §9.4's answer) + rewrite `ReferenceResolver`'s subscript
  interpreters (`HasDepth0Colon`/`InterpretSubscripts`/`SplitSubscriptTokens`/`RenderSegment`/`ResolveSubscriptName`)
  over real `arithmeticExpression`/`subscript` nodes. **COMMIT.**
- [ ] **Step D10.4 — REDUCED by P7 Step 12** (which already parses FUNCTION arguments as real trees through
  `functionArgList`, deleted the recursive-descent `SUB_*` parser, and routes UdfBinder/keyword-omitted through the
  ONE `BindArgOperand`). Residual scope: reunify `functionArgList` with `inlineMethodInvocationStatement`'s
  `argumentList` (one argument rule), and convert the keyword-omitted D2 channel from the `FunctionArgFragment`
  text re-parse to the interpreted-subscript grammar (falls out of D10.3, which makes a dataReference's captured
  subscripts real nodes). **COMMIT.**
- [ ] **Step D10.5 — delete the SUBSCRIPT-mode block** + the `LPAREN` mode-entry action + `PreviousTokenCouldBeDataName`
  + the now-dead structured `subscriptList/subscriptEntry/subscriptQualification/relativeOffset` rules (legacy is gone,
  so their `SubscriptEntryContext` consumer is gone); reconcile the PHASE-04 Group-A `cobol-words.json` drift test (the
  `subscriptTrigger` column goes dead — regenerate + adjust the drift assertion). **COMMIT.**
- **Verify (each step):** greenfield battery + `guard.ps1` + INV-1-strong; the D10.1 corpus green; token equivalence is
  NOT the metric (tokens change by design — prove OUTPUT/behavior equivalence). Exit criterion 0 holds when D10.5 lands.

#### CUT 3 — Runtime namespace flip (`CobolNet.Runtime` → `Cobol.Net.Runtime`)
This is the only step here that changes emitted code; it is a coordinated flip made trivial by the `RuntimeApi` façade.

- [ ] **Step 8 — Flip the runtime library's own namespace.**
  - Files: `src/Cobol.Net.Runtime/Cobol.Net.Runtime.csproj` — set `<RootNamespace>Cobol.Net.Runtime</RootNamespace>`
    and rewrite the banner (drop "renamed from CobolNet.Runtime at G0 … stays CobolNet.Runtime through G0-G7"; state the
    assembly and root namespace are now coherent). Then rewrite every `namespace CobolNet.Runtime` declaration across
    `src/Cobol.Net.Runtime/**/*.cs` to `namespace Cobol.Net.Runtime` (preserving any sub-namespace suffix, e.g.
    `CobolNet.Runtime.IO` → `Cobol.Net.Runtime.IO`). This is a mechanical find/replace of the exact token
    `namespace CobolNet.Runtime` → `namespace Cobol.Net.Runtime` plus internal `using CobolNet.Runtime…` → `using
    Cobol.Net.Runtime…` within the runtime project itself.
    - Sub-namespaces: DESIGN-runtime-library §2.8 says "realize the sub-namespaces" at this flip (`.Values/.IO/.Control/
      .Exceptions/.Intrinsics/.Verbs`). This is an OPEN QUESTION in the design (flat vs sub-namespaced). **Recommended
      for P15: flip the root token only (keep whatever sub-namespace structure P8's folder reorg left in place).**
      Deepening the namespace tree beyond the mechanical root flip is optional and, if done, must be reflected in the
      façade's emitted `using` set (step 9). Do the minimal, provably-one-using flip first; treat sub-namespace
      deepening as a follow-on only if the façade already emits fully-qualified member names.
  - Why: DESIGN-runtime-library §2.8 / §4 step 6. Assembly/namespace coherence; the deliberate deferral ends here.
  - Verify: `dotnet build src/Cobol.Net.Runtime/Cobol.Net.Runtime.csproj` succeeds; `grep -rn "namespace CobolNet.Runtime"
    src/Cobol.Net.Runtime ; echo "expect: NO hits"`. (The COMPILER will not build yet — it still emits/uses the old
    namespace until step 9. Do steps 8 and 9 in ONE commit.)

- [ ] **Step 9 — Flip the emitted `using` (one place) + any compiler-side runtime type references.**
  - Files: the `RuntimeApi` façade (`src/Cobol.Net.Compiler/CodeGen/.../RuntimeApi.cs`) — the single place that
    produces the generated program's `using CobolNet.Runtime;` (and every `Cobol*` runtime member name). Flip its
    emitted namespace constant(s) to `Cobol.Net.Runtime`. Then fix any direct compile-time references to runtime types
    inside the compiler (e.g. `using CobolNet.Runtime;` in binder/emitter files that call `ExceptionCatalog`,
    `CobolEdit.MaskCapacity`, `CobolDate.*`, `RoundingModes`, etc.) — these are `using` swaps to `Cobol.Net.Runtime`.
    Find them with `grep -rn "CobolNet.Runtime" src/Cobol.Net.Compiler`.
  - Why: DESIGN-runtime-library §2.8: "one file emits the `using`s, so generated `using CobolNet.Runtime;` flips to
    `using Cobol.Net.Runtime;` in exactly one place." The façade (P7) is precisely what makes this a one-emitted-change
    flip instead of corpus-wide churn.
  - Verify:
    ```bash
    dotnet build CobolSharp.sln
    # Compile a representative program and inspect the emitted .g.cs + run it.
    cat > /tmp/p15.cob <<'EOF'
    IDENTIFICATION DIVISION.
    PROGRAM-ID. P15NS.
    DATA DIVISION.
    WORKING-STORAGE SECTION.
    01 WS-N PIC 9(4) VALUE 42.
    PROCEDURE DIVISION.
    MAIN.
        DISPLAY "N=" WS-N.
        STOP RUN.
    EOF
    dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll /tmp/p15.cob --std 2023 -o /tmp/p15.dll --run
    grep -n "using Cobol.Net.Runtime\|using CobolNet.Runtime" /tmp/p15.g.cs 2>/dev/null || \
        grep -rn "using .*Runtime" /tmp  # confirm the .g.cs shows `using Cobol.Net.Runtime`
    ```
    Expected: builds clean; program prints `N=0042`; the emitted `.g.cs` shows `using Cobol.Net.Runtime` and **no**
    `using CobolNet.Runtime`; the runtime DLL deployed alongside is `Cobol.Net.Runtime.dll` (assembly name unchanged —
    only the namespace moved, so no deployment path changes).
  - Then the full battery:
    ```bash
    dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj
    dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj
    pwsh scripts/guard.ps1
    ```
    Expected: pass counts == §1 baseline (behavior-neutral: a namespace is a compile-time name only — runtime behavior
    and emitted-output bytes are identical). If the characterization/emit-snapshot gate (P0) flags the `.g.cs` `using`
    line as changed, that is the ONE expected, reviewed re-baseline — re-baseline the snapshots in THIS commit with a
    DEVLOG note ("emit change = runtime namespace flip; gate-1 output goldens prove behavior-neutral").
  - **COMMIT BOUNDARY** (steps 8 + 9 together — the tree does not build between them). Suggested message:
    `refactor(cobolnet)!: P15 Cut 3 — flip runtime RootNamespace CobolNet.Runtime → Cobol.Net.Runtime via the RuntimeApi façade (one emitted-using change)`

- [ ] **Step 10 — Regenerate `Generated/` from clean and confirm.**
  - `Generated/` is a build output (gitignored); a from-clean build must regenerate it without error and the emitted
    parser must be consistent with the flipped namespaces (the frontend's generated namespace is `CobolNet.Frontend.Generated`
    per P1/P4 — unaffected by the runtime flip, but re-verify a cold build).
  - Verify:
    ```bash
    dotnet clean CobolSharp.sln
    rm -rf src/Cobol.Net.Frontend/Generated
    dotnet build CobolSharp.sln           # regenerates Generated/, builds green
    pwsh scripts/guard.ps1                 # exit 0
    ```
  - No commit needed if nothing tracked changed (Generated/ is untracked). If the regen surfaced a tracked drift, fix
    and commit.

#### Conformance documentation + final doc pass

- [ ] **Step 11 — Author `docs/CONFORMANCE.md` (the §4.2.16 set).** See §7 for the required section map, sourcing, and
  the derive-don't-guess method. This is a large writing task; treat it as its own commit(s). Every claim cites a spec
  § and, where behavioral, a passing conformance test / golden. Cross-link from `README.md`.
  - Verify: the §7 checklist is fully satisfied; `grep -c "§" docs/CONFORMANCE.md` shows dense citation; the "claimed
    optional features" table matches what actually compiles+runs (spot-check ≥1 program per claimed A.4 subsection).
  - **COMMIT BOUNDARY.** Suggested message: `docs(cobolnet): P15 — publish the ISO §4.2.16 conformance / user documentation set (CONFORMANCE.md)`

- [ ] **Step 12 — Final DOC_INDEX + doc reconciliation.**
  - File: `docs/DOC_INDEX.md`. Remove rows for the deleted byte-engine architecture guides (already deleted at the
    PIVOT, but confirm no dangling rows), the deleted guard scripts (if indexed), and any doc that referenced the
    legacy engine as live. Add rows for `CONFORMANCE.md`, `LEGACY-ARCHIVE.md`, and this phase doc
    (Part II §PHASE-15). Update the preamble count. Also update this plan's §0 banner and `CLAUDE.md`'s PIVOT
    STATE line to record G8 complete (legacy severed; runtime namespace flipped; conformance doc published).
  - Verify:
    ```bash
    # every doc referenced in DOC_INDEX exists; no dangling links
    grep -oE "docs/[A-Za-z0-9_./-]+\.md|[A-Za-z0-9_-]+\.md" docs/DOC_INDEX.md | sort -u | \
        while read f; do [ -e "docs/$f" ] || [ -e "$f" ] || echo "MISSING: $f"; done
    ```
    Expected: no `MISSING:` lines.
  - **COMMIT BOUNDARY.** Suggested message:
    `docs(cobolnet): P15 — reconcile DOC_INDEX + live-state banners for G8 completion`

- [ ] **Step 13 — Final exit-criteria gate.** Run §5 in full; confirm all five EXIT CRITERIA hold; set STATUS to `DONE`.

---

#### The v1.0 RELEASE DEFINITION (D14 deliverable checklist — added 2026-07-19; QUALITY-tier items marked)

P15's exit is not only "the criteria hold" — it SHIPS v1.0. The release checklist (each item a small step,
most rolling into the existing conformance-doc pass):

1. **User documentation** (v1.0-required — "commercial-quality" + §4.2.16 imply it): a USER MANUAL covering
   installation, the CLI reference (`cobol` options, `check-batch`, `--std`/`--permissive` semantics), the
   per-edition language-support statement (generated FROM the traceability inventory — LIVE/DISPOSED rows),
   a migration-from-other-compilers note, and pointers to the error-message reference (`docs/DIAGNOSTICS.md`)
   and the conformance record (`docs/CONFORMANCE.md` + the A8 interpretations register).
2. **Packaging**: a `dotnet tool` package + release binaries (win-x64 / linux-x64), reproducible from a tag.
3. **Versioning + release engineering**: semantic versioning from v1.0.0; an annotated git tag; a CHANGELOG
   generated from the DEVLOG phase records; release notes stating the D13/D14 conformance claim verbatim.
4. **The release gate**: the full battery + the greenfield census guard + the §11 gates all green ON THE TAG.
(QUALITY-tier per D15: the manual's migration note and packaging polish never gate the conformance claim
itself — but items 1's language-support statement and 4 DO gate the v1.0 release.)

### 5. Verification (full battery at phase end)
Run all of these from a clean checkout on BOTH Windows and Linux (WSL) — cross-platform parity is now the whole gate:

```bash
dotnet clean CobolSharp.sln && dotnet build CobolSharp.sln            # green, warnings-as-errors in Release
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj   # == §1 baseline (~2028+)
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj                 # == §1 baseline (~213+)
pwsh scripts/guard.ps1                                                # exit 0 (greenfield NIST corpus all green)
bash scripts/version-continuity-sweep.sh | grep -q BREAKS && echo FAIL || echo "INV-1 OK"
```

Behavior-neutrality / byte-exact checks specific to P15:
- **Namespace flip is behavior-neutral:** the NIST golden set (`tests/nist/valid/*.txt`) and the differential goldens
  (`tests/differential/**/*.out`) must match byte-for-byte before and after Cut 3 — a namespace rename cannot change
  program output. The ONLY permitted diff is the emitted `.g.cs` `using` line (the P0 emit-snapshot gate re-baseline in
  step 9).
- **Legacy-delete is behavior-neutral:** Cut 1/Cut 2 change no `src/Cobol.Net.*` production code, so the battery counts
  must be identical to §1. A count change means an accidental deletion of something the greenfield tree needed —
  investigate before committing.
- **Grep gates (exit criterion 1):** the three grep sweeps in steps 6/7 return no live `CobolSharp.*` hits.

Equivalence-proof note: the authoritative equivalence proof (greenfield == legacy over the whole corpus) was produced
in P14 and is the gate that authorizes P15's irreversible deletes. P15 does not re-run it (legacy is being deleted);
it relies on P14's green result + the committed goldens as the frozen record of that equivalence.

---

### 6. Rollback / resumability
- **Resuming mid-phase:** read the STATUS line and the last `[x]` checkbox. Each step is its own commit boundary, so
  `git log --oneline | grep "P15"` shows exactly how far the phase got. Resume at the next `[ ]`.
- **Rollback of a namespace flip (Cut 3):** it is a mechanical rename; `git revert` the Cut-3 commit restores
  `CobolNet.Runtime` cleanly. Low risk.
- **Rollback of the engine delete (Cut 2):** the engine is at tag `legacy-byte-engine-final`. To restore, `git checkout
  legacy-byte-engine-final -- src/CobolSharp.Compiler src/CobolSharp.Runtime src/CobolSharp.CLI tests/CobolSharp.Tests.Unit
  tests/CobolSharp.Tests.Integration scripts/guard.sh` and re-add the sln entries. This is the documented recovery
  path; deletion is irreversible only in the sense that `main` no longer carries it.
- **Risks & mitigations:**
  - *Risk:* a hidden greenfield dependency on a legacy type surfaces only after Cut 2. *Mitigation:* the Cut-1 grep
    gate (step 1 verify) proves the compile-time graph is already legacy-free BEFORE Cut 2, so Cut 2 cannot break the
    build. If it does, a `using` alias to a `CobolSharp.*` type was hiding in a non-referenced-project file — restore
    from the tag, port the type into `Cobol.Net.*`, retry.
  - *Risk:* the namespace flip is more than one emitted place (the `RuntimeApi` façade is incomplete). *Mitigation:*
    step 9's `.g.cs` inspection catches a stray `using CobolNet.Runtime` immediately; if found, that member bypasses
    the façade — route it through `RuntimeApi` first (a P7 gap), then flip. Do NOT hand-flip generated strings scattered
    across the emitter; fix the façade.
  - *Risk:* a re-baselined emit snapshot masks a real behavioral change. *Mitigation:* gate-1 output goldens
    (NIST + differential `*.out`) are the authority; they must be untouched. Only the `using` line may move.
  - *Risk:* CI still gates on a deleted script/job. *Mitigation:* step 2 edits `build-and-test.yml` before Cut 2; push
    to a branch and confirm the workflow is green with only greenfield jobs before merging Cut 2.

---

### 7. ISO conformance documentation — `docs/CONFORMANCE.md` (spec sourcing + section map)
The ONLY ISO feature *work* in P15 is authoring the mandatory user documentation (§4.2.16). No new language features
are implemented here (they landed in P9–P13). The document is the artifact that lets the compiler *claim* conformance.

#### 7.1 What §4.2.16 requires
`specs/ISO_COBOL.md` §4.2.16 (line 2539): "An implementation shall satisfy the user documentation requirements
specified in **4.2.3, 4.2.4, 4.2.5, 4.2.6, 4.2.10, 4.2.12, and 4.2.13** by specification in at least one form of
documentation." Documentation may reference other documents. So the required set is:

| ISO § | Requirement | Source in this repo |
|---|---|---|
| §4.2.3 (line 2402) | Non-COBOL runtime-module interaction: document languages/implementations supported (or state none). | Runtime is typed-native .NET; document .NET interop stance (CALL to non-COBOL: supported/none). |
| §4.2.4 (line 2407) | Interaction between COBOL implementations: document implementations supported (or none). | Cross-vendor object/file interchange stance. |
| §4.2.5 (line 2422) → **A.1** (line 39232) | Implementor-defined language elements: document each supported; at minimum the ones A.1 marks *required*; document those A.1 marks as *requiring user documentation*. ~100 rows. | Derive from A.1 rows × what the compiler actually does (collating, default USAGE, currency, coded char set, line delimiter, arithmetic limits, etc.). |
| §4.2.6 (line 2431) → **A.3** (line 40052) | Processor-dependent elements: document those for which support is CLAIMED **and** the ABSENCE of those not supported (§4.2.6 last ¶: "The absence of processor-dependent elements … shall be specified"). | A.3 rows × claim/absence + `--std` gating. |
| §4.2.7 (line 2440) → **A.4** (line 40229) | Optional elements: identify those claimed; if partial, list supported vs not-supported parts. Includes **A.4.10 Object orientation**, **A.4.11 Report Writer**, **A.4.12 RESUME statement** (and A.4.2 screen, A.4.3 commit/rollback, A.4.4 dynamic-capacity tables, A.4.5 dynamic length, A.4.6 extended letters, A.4.7 sharing/locking, etc.). | A.4 subsection rows × implemented?/edition. |
| §4.2.10 (line 2466) | Nonstandard extensions claimed **and any reserved words added** for them. | The compiler's extension list (if any) + added reserved words from `reserved-words.json`. |
| §4.2.12 (line 2496) → **F.1** | Archaic elements present in the implementation. | F.1 rows × supported. |
| §4.2.13 (line 2505) → **F.2** | Obsolete elements present in the implementation. | F.2 rows × supported. |

Also include (good practice + referenced by the required sections): §4.2.2 the warning mechanism (`--std`, permissive/
strict), §4.2.8 reserved words recognized (§8.9), §4.2.9 standard extensions, §4.2.15 limits (max digits, table sizes),
§4.2.17 character substitution.

#### 7.2 Sourcing method — DERIVE, do not hand-guess (owner rule: cite the §)
The canonical machine-readable inputs already exist; the document must be generated/validated from them, not written
from memory:
- `tests/version-matrix/constructs.json` (+ P3's generated `docs/VERSION_CHANGE_REFERENCE.md`) — which constructs are
  introduced/removed/available at each edition. Every A.1/A.3/A.4/F.1/F.2 claim maps to construct rows here.
- `reserved-words.json` / `ReservedWords.Table` — for §4.2.8 recognized words and §4.2.10 added reserved words.
- The passing conformance tests + NIST goldens — a claim of "supported" MUST be backed by a green test. For each
  claimed A.4 subsection, cite ≥1 passing program (e.g. A.4.10 OO → `OoSpineTests`; A.4.4 dynamic-capacity tables →
  `OccursDynamicGuardTests`; A.4.11 Report Writer → `ReportWriterConformanceTests`; A.4.7 sharing/locking → the file
  I/O locking tests).
- `docs/ISO2023_CONFORMANCE_PLAN.md` — the M3/M4 pending list; anything still pending is documented as **not supported**
  (honest absence), not silently claimed. §4.2.6/§4.2.7 explicitly require documenting non-support.

Recommended: extend an existing generator (P3's `scripts/gen-vcr.ps1`) to emit the A.1/A.3/A.4/F.1/F.2 claim tables
from `constructs.json` + harness results so the conformance doc is regenerable and cannot silently drift from the
implementation (mirrors the `gen-reserved-words.ps1` discipline). A hand-written narrative wraps the generated tables.

#### 7.3 `docs/CONFORMANCE.md` section map (author to this outline)
1. **Title / scope / edition.** "ISO/IEC 1989:2023 conformance statement for COBOL.NET (cobol). Default `--std 2023`;
   supported editions 85/2002/2014/2023." Note conformance is claimed per the edition selected by `--std`.
2. **§4.2.2 Warning mechanism.** How to invoke conformance/extension/archaic/obsolete/nonstandard warnings (`--std`,
   `--permissive`/strict, the diagnostic codes band). Reference the diagnostic registry / `docs/DIAGNOSTICS.md`.
3. **§4.2.3 Non-COBOL interaction.** State the supported interop (typed-native .NET; CALL semantics) or explicit none.
4. **§4.2.4 COBOL-implementation interaction.** State supported cross-implementation interchange or explicit none.
5. **§4.2.5 / A.1 Implementor-defined elements.** The full A.1 table (row → the implementor's definition → cite the
   compiler behavior/spec §). Mark required rows. This is the ~100-item core.
6. **§4.2.6 / A.3 Processor-dependent elements.** Table of claimed (with the syntax/functionality subset if a standard
   extension) AND a table of **absences** (explicitly not supported).
7. **§4.2.7 / A.4 Optional elements.** Per A.4 subsection: claimed? fully/partially? at which editions? backing test.
   MUST include A.4.10 (OO), A.4.11 (Report Writer), A.4.12 (RESUME) rows.
8. **§4.2.9 Standard extensions / §4.2.10 Nonstandard extensions.** The extension list + any reserved words added
   (§4.2.10 last ¶ mandates specifying added reserved words).
9. **§4.2.12 / F.1 Archaic** and **§4.2.13 / F.2 Obsolete** elements present in the implementation.
10. **§4.2.15 Limits.** Max numeric digits (standard/extended), table dimensions, nesting, etc.
11. **§4.2.17 Character substitution & §8.1.3 repertoire.** The coded character set (UTF-16 native), any substitutions.
12. **References.** The spec, the version matrix, the diagnostics doc, the test corpus.

#### 7.4 Conformance tests / goldens to add in P15
No new *language* goldens (features are frozen). Add instead a **documentation-integrity test** so the conformance
claims cannot rot:
- `tests/Cobol.Net.Tests.Conformance/ConformanceDocDriftTests.cs` — asserts every A.4 subsection the doc marks
  "supported" has a named passing test, and every construct the doc marks "not supported" is `pending`/absent in
  `constructs.json`. This binds `CONFORMANCE.md` to the harness the same way `CorpusManifestTests` binds `corpus.tsv`
  (DESIGN-test-build-ci §3.5). Green = the doc's claims match reality.

---

### 8. Commit / DEVLOG discipline (per project rules)
Every commit boundary above gets a DEVLOG entry at the TOP of `DEVLOG.md` (descending; real `date "+%Y-%m-%d %H:%M %Z"`
stamp; `## Entry NNN — … — Title`), referencing "P15 / G8". Commit messages are forensic and end with the mandated
Co-Authored-By / Claude-Session trailers. Push every checkpoint (fully-autonomous rule). After the phase, update
this plan's §0 + `CLAUDE.md` PIVOT STATE to "G8 COMPLETE".

# PHASE-16 — CIL/Cecil backend + backend-neutrality proof

### 1. Preconditions & how to resume

Before starting, confirm P6/P7 landed the neutral seam (grep to verify — all must exist):

```bash
cd E:/CobolSharp
grep -rln "interface ICodeGenBackend"     src/Cobol.Net.Compiler/CodeGen   # P7 Step 1 seam
grep -rln "record BoundCompilation"        src/Cobol.Net.Compiler/Binding   # P6 immutable result
grep -rln "record AccessPath"              src/Cobol.Net.Compiler/Binding   # P7 Step 11 structural Place
grep -rln "class RuntimeAbi"               src/Cobol.Net.Compiler           # neutral runtime catalogue (DESIGN-backend-abstraction §2.3)
grep -rln "class NameMangler"              src/Cobol.Net.Compiler/Model     # shared naming service (§2.4)
# The bound tree must be neutral: these must return NOTHING (L3/L4 de-C#'d in P6):
grep -rn  "string TablePath\|string IndexField\|string CsName\|string SendingPath" \
          src/Cobol.Net.Compiler/Binding/Bound src/Cobol.Net.Compiler/Binding/Model/Place.cs
grep -rn  "Read()\|Write(" src/Cobol.Net.Compiler/Binding/Model/Place.cs         # must be gone (P7 Step 11)
```

If the seam or the structural `Place` is missing, STOP and finish P6/P7 first. If the last two greps return hits, the
**bound tree is not yet neutral** — the CIL backend cannot consume it; finish P6's L3/L4 de-C#-ing
(`DESIGN-backend-abstraction.md §1.3`) first.

**The battery (run at every commit boundary; must stay green):**

```bash
# 1. Greenfield conformance (~2003 cases). Run with BOTH backends from Milestone 3 on (§ CLI matrix).
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -v quiet
# 2. Greenfield unit (~213).
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj -v quiet
# 3. Backend-contract neutrality test (DESIGN-backend-abstraction §6) — MUST be green from Milestone 0.
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj --filter Category=BackendContract -v quiet
# 4. Backend-equivalence harness (NEW this phase) — Roslyn vs Cil stdout byte-compare over the enabled subset.
dotnet test tests/Cobol.Net.Tests.BackendEquivalence/Cobol.Net.Tests.BackendEquivalence.csproj -v quiet
# 5. FULL legacy differential guard — only if a SHARED .g4 is touched (this phase touches NONE) + once at phase end.
bash scripts/guard-fast.sh
```

**Behavioral probe (prebuilt CLI, both backends):**

```bash
dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll <source.cob> --std 2002 -o /tmp/r.dll --backend roslyn --run
dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll <source.cob> --std 2002 -o /tmp/c.dll --backend cil    --run
diff <(dotnet /tmp/r.dll) <(dotnet /tmp/c.dll) && echo "BYTE-IDENTICAL"
```

**Resuming mid-phase:** every step is an independent COMMIT BOUNDARY leaving the battery green. To resume, read the
STATUS line, `git log --oneline | grep "P16"` for the last landed sub-commit, continue at the next. No step leaves the
tree non-compiling at its boundary. Milestones 3–6 are multi-sub-commit; each carries its own resumability note (§6).

---

### 2. Rationale — the problem this phase fixes

The rearchitecture plan names the dual-backend seam (`DESIGN-codegen-backend.md §2.2`) and makes `Place` structural
(P7 Step 11), but **no phase actually builds or even seam-proves a second backend**. Consequences the plan leaves
open:

1. **The seam is untested by a real consumer.** `ICodeGenBackend` with one implementation is an interface, not a
   proven abstraction — nothing forces the tree to be genuinely neutral. A single C# string can creep back into a
   node (as `BoundSetCapacity.TablePath` `BoundTree.cs:516` and `BoundSearch` `BoundTree.cs:531` show today) and
   nothing fails until someone tries to write the second backend, years later.
2. **The owner-emphasized goal has no delivery vehicle.** `project_dual_backend_goal` is a durable directive ("a
   direct CIL/IL backend must be droppable in later WITHOUT touching the frontend, binder, or bound tree"). The plan
   must contain the phase that delivers it and the milestone that proves it early.
3. **Neutrality rots silently without a live second consumer.** `DESIGN-codegen-backend.md §6 R5` foresaw this and
   proposed a reflection test; this phase makes the guarantee **executable** — an actual non-C# backend consumes the
   tree (Milestone 0), so residual C# is caught by construction, not just by a reflection heuristic.

This phase closes all three: an early seam-proof (dependency-free), the production Cecil backend, an equivalence
harness, and CI on both backends — additive, Roslyn-default, battery-green throughout.

---

### 3. Target end-state for this phase (concrete)

When P16 is DONE, these exist with these responsibilities (grounded in `DESIGN-backend-abstraction.md §2–§4`):

**Seam-proof (Milestone 0 — in-box, dependency-free)**
- `CodeGen/NullBackend.cs` — `NullBackend : ICodeGenBackend` — consumes `BoundCompilation`, produces a
  `BackendArtifact(Success:true, …, AssemblyPath:null)` doing nothing; proves the driver→seam plumbing.
- `CodeGen/DisplayBackend.cs` — `DisplayBackend : ICodeGenBackend` over **`System.Reflection.Emit`
  `PersistedAssemblyBuilder`** (.NET 10 in-box) — lowers ONLY `BoundProgram`s whose statements are `BoundDisplay` of
  literal/field operands + `BoundStop`, emitting `Console.WriteLine` IL; every other node → a loud `NotSupported`.
  The dependency-free proof that a non-C# backend consumes the neutral tree.
- The backend-contract test's **assertion 3** (`DESIGN-backend-abstraction.md §6`) round-trips a DISPLAY-only
  `BoundCompilation` through Roslyn AND `DisplayBackend`, asserting byte-identical stdout.

**Production CIL backend (Milestones 1–6 — Mono.Cecil, isolated assembly)**
- assembly `src/Cobol.Net.Backend.Cil/` (refs `Cobol.Net.Compiler`, `Cobol.Net.Runtime`, `Mono.Cecil`) — the ONLY
  place the Cecil dependency lives (`DESIGN-backend-abstraction.md §3.2`).
- `CilBackend.cs` — `CilBackend : ICodeGenBackend`; drives `CilProgramEmitter` per unit; writes the PE + portable PDB
  + `.runtimeconfig.json`.
- `CilProgramEmitter.cs` — one program/class/interface unit → its `TypeDefinition` + the `__Dispatch` method + entry
  wrapper (the same shape the Roslyn `ProgramEmitter`/`DispatchEmitter` emit, in IL).
- `CilStatementEmitter : IBoundStatementVisitor<CilFlow>`, `CilExpressionEmitter : IBoundExprVisitor<CilVal>`,
  `CilConditionEmitter : IBoundConditionVisitor<CilBranch>`, `CilBoolEmitter`, `CilPlaceLower` (structural `Place` →
  ldfld/stfld/ldelema/call), `CilDispatcher` (the PC `while(true)switch` lowered to a branch table),
  `CilRuntimeApi` (Cecil `MethodReference`s over the shared `RuntimeAbi`).
- The visitor implementations INHERIT exhaustiveness from the source-generated interfaces (P7 Step 6) — a new bound
  node is a compile error in the CIL backend too.

**Wiring & tests**
- `Cli/CliOptions.cs` + `CompilerDriver.Options` (`CompilerDriver.cs:34`): `BackendId Backend = BackendId.Roslyn`;
  `Cli/Program.cs`: `--backend {roslyn|cil}` option; `CompilerDriver.Compile` resolves via
  `BackendFactory.For(options.Backend, cilPlugin)`.
- `tests/Cobol.Net.Tests.BackendEquivalence/` — the harness (§ Milestone 2); a per-program `[Theory]` over the
  enabled corpus subset, byte-comparing `dotnet r.dll` vs `dotnet c.dll`.
- CI: the conformance job runs the enabled subset under `--backend cil` in addition to `--backend roslyn`.

---

### 4. STEP-BY-STEP

> Ordering principle: **seam-proof first (cheap, in-box), then the Cecil backend feature-by-feature, each gated by the
> equivalence harness.** Every milestone step is a COMMIT BOUNDARY; run battery items 1–4 at every boundary (item 5
> only at phase end). The CIL backend is additive: a not-yet-implemented node lowers to a loud `NotSupported`, and the
> equivalence harness only enables a program once every node it uses is implemented — so the battery is never red for
> "CIL doesn't do X yet".

#### Milestone 0 — the seam-proof (recommended: execute as PHASE-07 Step 13)

##### Step 0.1 — `NullBackend` + `BackendFactory` plumbing

- **Files:** create `CodeGen/NullBackend.cs`; edit `CodeGen/ICodeGenBackend.cs` (`BackendFactory`), `CompilerDriver.cs`.
- **Change:** `NullBackend : ICodeGenBackend` returns `BackendArtifact(true, [], null, null)`. Add a hidden
  `--backend null` (test-only) so `CompilerDriver` can route a `BoundCompilation` to it. Proves the P6→P7 seam
  actually carries a `BoundCompilation` end-to-end with zero C# rendering.
- **Verify:** `cobol x.cob --backend null` returns success and writes nothing; battery 1+2+3 green.
- **COMMIT:** `P16 step0.1: NullBackend + BackendFactory routing (seam carries BoundCompilation end-to-end)`

##### Step 0.2 — in-box `DisplayBackend` (Reflection.Emit, DISPLAY-only)

- **Files:** create `CodeGen/DisplayBackend.cs`.
- **Change:** `DisplayBackend : ICodeGenBackend` over `System.Reflection.Emit.PersistedAssemblyBuilder` (.NET 10
  in-box — no external dependency, `DESIGN-backend-abstraction.md §3.3`). Walk `BoundCompilation`; for a
  `BoundProgram` whose statements are `BoundDisplay` (of `BoundStringLiteral`/`BoundNumericLiteral`/simple
  `BoundFieldOperand`) and `BoundStop`, emit a `Main` that `call`s `Console.WriteLine`/`Console.Write`; every other
  node throws `NotSupportedException` at emit (loud, never silent). Save a runnable `.dll` + `.runtimeconfig.json`.
- **Why:** the dependency-free proof that a **non-C#** backend consumes the neutral tree. If any node still exposed a
  C# string, this backend could not be written — that is the point.
- **Verify:** `cobol hello.cob --backend display -o /tmp/h.dll --run` prints the same as `--backend roslyn`;
  battery 1+2+3 green.
- **COMMIT:** `P16 step0.2: in-box Reflection.Emit DisplayBackend (DISPLAY-only) — seam proven by a non-C# consumer`

##### Step 0.3 — the backend-contract test, assertion 3 (two real consumers)

- **Files:** create `tests/Cobol.Net.Tests.Unit/BackendContract/` fixtures (assertions 1–2 land in P7 Step 11; this
  adds assertion 3, `DESIGN-backend-abstraction.md §6`).
- **Change:** a `[Category("BackendContract")]` test builds a fixed DISPLAY-only `BoundCompilation` and runs it
  through `RoslynBackend` AND `DisplayBackend`, asserting byte-identical stdout. Now a residual C# string in any node
  used by that program breaks the build.
- **Verify:** battery 1+2+3 green; the test is a hard CI gate.
- **COMMIT:** `P16 step0.3: backend-contract test assertion 3 (Roslyn≡Display on a neutral DISPLAY program)`

> **Milestone 0 exit:** the seam is real, and neutrality is proven by a second consumer + an executable test — the
> anti-rot guarantee. Everything below is the production backend and is **out of P7**; it can proceed whenever the CIL
> backend is scheduled without ever un-proving the seam.

#### Milestone 1 — the `Cobol.Net.Backend.Cil` assembly + a Cecil DISPLAY-only backend

##### Step 1.1 — create the isolated assembly + `CilBackend` skeleton

- **Files:** create `src/Cobol.Net.Backend.Cil/Cobol.Net.Backend.Cil.csproj` (refs `Cobol.Net.Compiler`,
  `Cobol.Net.Runtime`, `Mono.Cecil` NuGet); `CilBackend.cs`; add the project to the solution and to the CLI's plug
  wiring (`BackendFactory.For(BackendId.Cil, new CilBackend())`).
- **Change:** `CilBackend : ICodeGenBackend` with a `ModuleDefinition` targeting `net10.0`, a `Program`
  `TypeDefinition`, an empty `Main`; writes the PE + a portable PDB + `.runtimeconfig.json` (reuse the shared
  `AssemblyPackager.WriteRuntimeConfig` JSON shape, `AssemblyPackager.cs:40-54`). No statements yet — a
  non-DISPLAY program lowers to a loud `NotSupported`.
- **Why:** stands up the isolated Cecil assembly (default path stays Cecil-free, `DESIGN-backend-abstraction.md §3.2`)
  and the PE/PDB writer.
- **Verify:** the solution builds; `cobol hello.cob --backend cil` produces a loadable (empty-Main) assembly;
  battery 1+2+3 green (CIL not yet in the equivalence subset).
- **COMMIT:** `P16 step1.1: Cobol.Net.Backend.Cil assembly + CilBackend skeleton (PE + portable PDB writer)`

##### Step 1.2 — `CilRuntimeApi` over the shared `RuntimeAbi`; `CilPlaceLower` (read); DISPLAY

- **Files:** create `CilRuntimeApi.cs`, `CilPlaceLower.cs`, `CilStatementEmitter.cs` (partial — DISPLAY + STOP),
  `CilExpressionEmitter.cs` (partial — field/literal read).
- **Change:** `CilRuntimeApi` imports `MethodReference`s from `RuntimeAbi` (`DESIGN-backend-abstraction.md §2.3`).
  `CilPlaceLower.Read(Place)` lowers a `MemberPlace` `AccessPath` to `ldsfld`/`ldfld`/`ldelem` (the SENDING side).
  `CilStatementEmitter` implements `Visit(BoundDisplay)`/`Visit(BoundStop)` → `Console.Write*` + the runtime image
  calls (`CobolNum.FormatDisplay` via `CilRuntimeApi`). Names come from the shared `NameMangler`.
- **Verify:** DISPLAY of a group/field/numeric matches Roslyn byte-for-byte (probe below); battery 1+2+3 green.
  ```bash
  diff <(dotnet /tmp/r.dll) <(dotnet /tmp/c.dll)   # for a DISPLAY-of-fields program
  ```
- **COMMIT:** `P16 step1.2: CilRuntimeApi + CilPlaceLower read + DISPLAY/STOP (byte-matches Roslyn)`

#### Milestone 2 — the backend-equivalence harness (the growth engine)

- **Files:** create `tests/Cobol.Net.Tests.BackendEquivalence/BackendEquivalenceTests.cs` + an
  `enabled-cil-corpus.txt` manifest (starts with the DISPLAY-only programs).
- **Change:** a `[Theory]` over the manifest: for each program, `CompilerDriver.Compile` with `Backend=Roslyn` → run
  → capture stdout/stderr/exit; same with `Backend=Cil`; assert **byte-identical** all three. A program enters the
  manifest ONLY when every bound-node kind it uses is implemented in the CIL backend (so the harness is always green;
  it grows as milestones land). Provide a `scripts/cil-corpus-add.sh <glob>` helper that trial-runs candidates and
  reports which pass, to grow the manifest safely.
- **Why:** the single mechanism that keeps CIL provably equal to Roslyn as features land; it is the CIL backend's
  "differential oracle" (`DESIGN-codegen-backend.md §2` bonus — the two backends cross-check each other).
- **Verify:** the harness is green over the DISPLAY-only manifest; battery 1+2+3+4 green.
- **COMMIT:** `P16 step2: backend-equivalence harness (Roslyn≡Cil stdout) + growable enabled-corpus manifest`

#### Milestone 3 — numerics + moves (multi-sub-commit)

- **Files:** extend `CilExpressionEmitter` (full `IBoundExprVisitor<CilVal>`: `BoundBinary`/`BoundNegate`/
  `BoundPower`/`BoundNumLiteral` scaled-integer lowering per SSOT §1.2 #2 — `long`/`Int128`), `CilPlaceLower.Write`,
  `CilStatementEmitter` MOVE + arithmetic (`BoundMove` reading `MoveKind`+`StorageForm` from the node — P7 Step 7;
  `BoundAddTo`/…/`BoundCompute` with `ReceiverContext` scale/rounding lowered to IL).
- **Change:** lower the scaled-integer numeric pipeline to IL (unscaled value in a native int; scale is compile-time
  metadata — the CIL emitter aligns scales exactly as the Roslyn `NumX` render does, over the SAME `RuntimeAbi`
  numeric members). MOVE per `MoveKind` (group/elementary/edited/figurative-fill/ref-mod-slice) — a pure switch on
  the node, no re-classification (P7 Step 7 already moved that onto the node). Add the newly-covered programs to the
  equivalence manifest.
- **Verify:** equivalence harness green over the numerics+moves subset; probe COMPUTE/divide/ROUNDED/edited-MOVE
  programs byte-identical.
- **COMMIT (per sub-group):** `P16 step3X: CIL <numerics|moves|arithmetic> lowering; grow equivalence corpus`

#### Milestone 4 — control flow + the PERFORM dispatcher (multi-sub-commit) — the HIGH-risk core

- **Files:** create `CilDispatcher.cs`; extend `CilStatementEmitter` (`BoundIf`, `BoundInlinePerform`,
  `BoundOutOfLinePerform`, `BoundGoTo`, `BoundGoToDepending`, `BoundExitParagraph`/`Perform`, `BoundEvaluate`,
  `BoundNextSentence`, `BoundSearch`, SET index/capacity, ALTER).
- **Change:** lower the single PC dispatcher (SSOT §1.2 #3; the Roslyn `while`/`switch(__pc)` dispatcher
  `DispatchEmitter.cs:76-104`) to an **IL branch table** — this is the CIL backend's **private** structure→branch
  lowering (SSOT §1.1: NOT a shared phase). `GO TO` sets `__pc` + branches to the dispatch head; an out-of-line
  PERFORM is a recursive bounded `__Dispatch(start,end)` IL call; inline PERFORM/EVALUATE lower to loops/branch
  chains. `BoundSearch`/`BoundSetCapacity` consume the **symbol/`Place`** the node now carries (P6 L3 de-C#-ing),
  not a path string.
- **Why:** the dispatcher is the backbone every non-trivial program uses; getting it byte-equal is the make-or-break
  of the backend.
- **Verify:** equivalence harness green over control-flow programs (PERFORM THRU, VARYING/AFTER, GO TO DEPENDING,
  EVALUATE, SEARCH ALL). This is where the harness earns its keep — byte-identical stdout on branch-heavy programs.
- **COMMIT (per sub-group):** `P16 step4X: CIL <dispatcher|PERFORM|EVALUATE|SEARCH> branch lowering; grow corpus`

#### Milestone 5 — file I/O (multi-sub-commit)

- **Files:** extend `CilStatementEmitter` (`BoundOpen`/`Close`/`Read`/`Write`/`Rewrite`, keyed I/O, SORT/MERGE,
  Report Writer verbs, `BoundUnlock`), reusing the SAME `Cobol.Net.Runtime.IO` façade the Roslyn backend calls (via
  `CilRuntimeApi` over `RuntimeAbi`). File-connector keys come from the neutral `FileModel` (the emit-side
  qualification in `Verbs/CallEmitter.cs` is a P6/driver concern, not a node string).
- **Change:** lower OPEN/READ/WRITE/CLOSE + FILE STATUS + USE declaratives to IL calls into the runtime file system;
  no new runtime code (the runtime is shared). Grow the manifest with the SQ/RL/IX corpus families.
- **Verify:** equivalence harness green over the file-I/O subset (compare stdout AND any produced data files
  byte-for-byte).
- **COMMIT (per sub-group):** `P16 step5X: CIL <sequential|keyed|sort|report-writer> I/O lowering; grow corpus`

#### Milestone 6 — OO + the EC exception model (multi-sub-commit)

- **Files:** extend `CilProgramEmitter` (class/factory/interface `TypeDefinition`s, `__CobolInvoke` dispatch),
  `CilStatementEmitter` (`BoundInvoke*`, `BoundSetObjectRef`, `BoundRaise`/`Resume`/`RaiseObject`/`Raising`,
  `BoundEcChecked` → IL try/finally, pointers/`ALLOCATE`/`FREE`).
- **Change:** lower the OO type model (the method names via the shared `NameMangler`, the `OoMethodSymbol` the node
  carries after L4 removal — not `BoundMethod.CsName`) and the EC machinery (try/finally + the runtime
  `ExceptionState`/`__EcDispatch` calls via `RuntimeAbi`). This is the last feature family; at its end the manifest
  can grow toward the full corpus.
- **Verify:** equivalence harness green over the OO + EC subset (IC/OBSQ families, `>>TURN`, RAISE/RESUME, USE F3).
- **COMMIT (per sub-group):** `P16 step6X: CIL <OO dispatch|EC model|pointers> lowering; grow corpus`

#### Milestone 7 — CLI + CI on both backends

- **Files:** `Cli/CliOptions.cs`, `Cli/Program.cs`, `CompilerDriver.cs`; CI workflow.
- **Change:** add `--backend {roslyn|cil}` (default `roslyn`) to the System.CommandLine surface; thread
  `BackendId Backend` through `CompilerDriver.Options` (`CompilerDriver.cs:34`); `CompilerDriver.Compile` resolves the
  backend via `BackendFactory.For(options.Backend, cilPlugin)`. In CI, add a matrix leg that runs the **enabled CIL
  subset** of the conformance corpus under `--backend cil` (byte-comparing to the golden AND to the Roslyn output via
  the equivalence harness). Roslyn remains the default and the full-corpus authority.
- **Verify:** `cobol … --backend cil` and `--backend roslyn` both work from the shipped CLI; CI green on both legs.
- **COMMIT:** `P16 step7: --backend {roslyn|cil} CLI wiring + CI matrix on both backends`

---

### 5. Verification (phase end)

Run the COMPLETE battery and confirm all green + neutral + equivalent:

```bash
cd E:/CobolSharp
dotnet build Cobol.Net.sln -v quiet                                                   # all projects incl. Backend.Cil
dotnet test  tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -v quiet     # Roslyn: ~2003 green, 0 diffs
dotnet test  tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj -v quiet                   # incl. BackendContract
dotnet test  tests/Cobol.Net.Tests.BackendEquivalence/Cobol.Net.Tests.BackendEquivalence.csproj -v quiet  # Roslyn≡Cil subset
bash scripts/guard-fast.sh                                                             # legacy guard (untouched — no .g4 change)
```

Exit checks (all must pass):

1. **Byte-equivalence over the defined subset.** The equivalence manifest covers the target subset (grown from
   DISPLAY-only to the full feature families in Milestones 3–6); every entry's Roslyn and Cil stdout/stderr/exit are
   byte-identical. The subset's size and the growth-to-full plan are recorded in the manifest header.
2. **The neutrality contract is green (proven by construction).** The backend-contract test
   (`DESIGN-backend-abstraction.md §6`) passes: no `Place`/`Bound*` node exposes a `string`-returning render member
   or a raw-C#-identifier field, AND a non-C# backend (`DisplayBackend`, and now `CilBackend`) consumes the tree — the
   executable proof that the IR carries no C#.
3. **Roslyn is untouched as default.** `--backend roslyn` is the default; every pre-existing battery item is green;
   the Cecil dependency exists ONLY in `Cobol.Net.Backend.Cil` (grep: `Mono.Cecil` appears in no other csproj).
4. **A missing node is a compile error in BOTH backends.** Add a throwaway `sealed record BoundProbe : BoundStatement;`
   → `dotnet build` fails in `StatementEmitter` AND `CilStatementEmitter` (the source-generated exhaustiveness, P7
   Step 6, inherited by the CIL visitor). Remove the probe; do not commit it.

---

### 6. Rollback / resumability

- **Every step is an independent, battery-green commit.** Resume: read STATUS, `git log --oneline | grep "P16"` for
  the last landed sub-commit, continue at the next.
- **Milestone 0 is the anti-rot floor.** Even if Milestones 1–6 are deferred indefinitely, the seam-proof
  (`NullBackend` + `DisplayBackend`) + the contract test keep neutrality enforced with NO Cecil dependency. Reverting
  the Cecil milestones never un-proves the seam.
- **CIL is additive — a partial backend never breaks the battery.** An unimplemented node lowers to a loud
  `NotSupported`, and the equivalence manifest only enables a program once every node it uses is implemented. So a
  half-built CIL backend leaves the manifest smaller, never red.
- **Milestone 3–6 internal resume:** each is per-feature-family sub-commits; the enabled-corpus manifest records
  exactly which families are live, so the resume point is "the first family not yet in the manifest".
- **Risks & mitigations** (from `DESIGN-backend-abstraction.md §7`):
  - **R — CIL control-flow parity (HIGH, Milestone 4).** The private dispatcher branch lowering must match the
    Roslyn `while(true)switch` byte-for-byte in behavior. Mitigation: the equivalence harness on branch-heavy
    programs; land Milestone 4 in small sub-commits, one construct at a time (`feedback_tiered_gates`).
  - **R — `RuntimeAbi` overload identity (MEDIUM).** A Cecil `MethodReference` import must pick the exact overload.
    Mitigation: `RuntimeMember` carries a parameter-shape key; `CilRuntimeApi` throws loudly on an ambiguous import.
  - **R — PDB / debug-info scope (LOW for correctness).** Portable-PDB sequence points are a quality goal, not a
    correctness one; the equivalence harness compares runtime behavior, not debug info. Defer full source-line PDBs
    to a follow-on if needed (`DESIGN-backend-abstraction.md §7` Open Q2).
  - **R — neutral tree regressions from later phases.** Any phase after P7 that adds a bound node with a C# string
    field fails the backend-contract test (§ Verification #2) — caught in CI, not years later.

---

### 7. ISO feature work in this phase

**None.** P16 is a **backend-additive** phase — it adds NO ISO construct and changes NO observable output of the
default (Roslyn) backend. The CIL backend's entire correctness criterion is *byte-equality with the Roslyn backend*
over the enabled corpus subset; it introduces no new semantics, no new goldens (it re-uses the existing conformance
goldens through the equivalence harness), and touches no `.g4` (no legacy-guard exposure). The four owner-locked
invariants (SSOT §1.2) and "no shared lowered IR" (§1.1) are upheld: the CIL backend does its structure→branch
lowering **privately**, and the bound tree it consumes is the SAME neutral tree the Roslyn backend consumes.

---

### Appendix A — file/line anchors (AS-IS, for the executing session)

| Concern | AS-IS location |
|---|---|
| The `ICodeGenBackend` seam (CIL adds a sibling impl) | `CodeGen/ICodeGenBackend.cs`; `CodeGen/RoslynBackend.cs:16` (`RoslynBackend : ICodeGenBackend`; refs cached `:95-98`) |
| Bind/emit split (P6-extracted) | `Binding/BinderDriver.cs` → `BoundCompilation`; `CodeGen/CSharpEmitter.cs:31,39` = the `Bind`/`EmitBound` bind-host facade (emit lives in `ProgramEmitter`/`DispatchEmitter`/`Verbs/*Emitter`) |
| `Place.Read()/Write()` C# strings (P7 Step 11 removes) | `Binding/Model/Place.cs:22-25` + every subtype |
| C#-path node fields (P6 L3/L4 de-C#, `DESIGN-backend-abstraction §1.3`) | `Binding/Bound/BoundTree.cs:34` (`BoundMethod.CsName`), `:112` (`BoundIndexRef.IndexField`), `:494` (`SetIndexTarget.IndexField`), `:516` (`BoundSetCapacity.TablePath`), `:531` (`BoundSearch`), `Binding/Model/Place.cs:59,176` |
| Shared `WriteRuntimeConfig` JSON (share with CIL) | `CodeGen/AssemblyPackager.cs:40-54` |
| The PC dispatcher (Roslyn `while`/`switch(__pc)` → CIL branch table) | `CodeGen/DispatchEmitter.cs:76-104` |
| Exhaustive visitor interfaces (CIL visitors inherit exhaustiveness) | P7 Step 6 (`Binding/Bound/BoundTree.cs` `[BoundNode]`) |
| `RuntimeAbi` catalogue (planned) / the current `RuntimeApi` static class (CIL adds `CilRuntimeApi`) | `DESIGN-backend-abstraction.md §2.3`; `CodeGen/Roslyn/RuntimeApi.cs`; runtime members `grep -hoE "Cobol[A-Za-z]+\." src/Cobol.Net.Compiler/CodeGen` |
| CLI options (add `--backend`) | `Cli/CliOptions.cs`, `Cli/Program.cs`, `CompilerDriver.Options` `CompilerDriver.cs:34` |

---

# PART III — COMPLETED-PHASE RECORDS (STATUS banners absorbed verbatim; full history in DEVLOG)

### PHASE-00 record (absorbed from the retired phase doc)

> `DONE` (2026-07-07) — all 13 steps complete. Full §5 battery GREEN: sln build 0-err (warnings-as-errors) · 2036
> greenfield conformance · 213 unit · 32 characterization · differential verify 644/0 (cobolnet==legacy==golden) · FULL
> legacy guard NIST 353 MATCH. NEXT: Phase 01 (mechanical namespace rename + dead-grammar/JSON-XML removal). Baseline
> green (2036 conformance · 213 unit · guard 353 MATCH). Characterization
> net complete (gates 2+3, 32 tests). Step 7: `tests/nist/corpus.tsv` generated mechanically — 459 rows (338 green + 11
> divergent + 110 pending; folds `[InlineData]` 349 + `chains.tsv` + `LEGACY_DIVERGENT`) + `CorpusManifest` loader + a
> 5-assertion drift guard (green∪divergent == the committed 349-name baseline; every green/divergent has a golden; every
> divergent cites §; no dupes; every on-disk program listed) — all green. Step 8: `NistDifferentialTests` repointed at
> `[MemberData(CorpusManifest.GreenData)]` + `CorpusManifest.Chains` (487-line `[InlineData]` block + the private `Chains`
> lazy deleted) — **349/349 pass, verdict-equivalent**. Step 9: `DifferentialGolden` helper (bake/verify/golden modes;
> content-hash goldens under `tests/differential/<TestClass>/<hash>.out`; edition+source hash so many `[InlineData]`
> sources per funnel never collide) — builds clean. Step 10: 46-file conversion (parallel workflow, one agent/file, 0
> errors) — 42 funnels routed through `DifferentialGolden.Assert` + 360 goldens baked under `tests/differential/<Class>/`;
> 4 files correctly untouched (MoveEdition/SignedAlphanumericMove/AllLiteral/AbbreviatedCondition — spec-pinned-only, no
> `lout==cout` funnel); fields kept where a spec-pinned funnel still uses them (ClassCondition/ControlFlow/…). Bake 644/0,
> golden-mode 644/0 (no legacy). Step 11: `DifferentialGoldenDriftTests` (folder-level orphan guard — every converted
> class has a non-empty golden folder; every golden folder maps to a live converted class; 3/3 green) + completeness
> sweep: a fresh whole-suite re-bake left `git status tests/differential` CLEAN → the 360 goldens are complete and
> idempotent (byte-identical regen). Step 12: CI wired (`build-and-test.yml`) — characterization compare-gate added to
> `greenfield-tests` + `windows-build-test` (CI never sets UPDATE_SNAPSHOTS); new opt-in `legacy-oracle` job runs
> differential `COBOLNET_DIFF_MODE=verify` (cobolnet==legacy==golden) NIGHTLY + on manual dispatch only (not every PR — the
> normal battery stays legacy-free); legacy `guard`/`inv1-sweep` untouched. Local CI-equivalents green: characterization
> 32/32 compare, verify 644/0. `chains.tsv` KEPT (bash guard/sweep still read it). Step 2 ref-cache ~40–55% faster.
> Finding for PHASE-02: undefined-data-name + JUSTIFIED-on-numeric not caught at bind time.
> Step 1 (migration-SSOT banner) DONE: the migration SSOT is `docs/COBOLNET_REARCHITECTURE_PLAN.md` (the master plan —
> STATE banner + P0–P16 index + exit criteria + the resume-vs-migration pointer); its banner is flipped to "P0 IN
> PROGRESS" and `docs/DOC_INDEX.md` carries its row.
> <!-- The executing session updates this line: NOT STARTED  |  IN PROGRESS @ step N  |  DONE.
>      Keep a one-line note of the last green commit hash when you pause. -->
> 
> ---


### PHASE-01 record (absorbed from the retired phase doc)

> (no STATUS block found)


### PHASE-02 record (absorbed from the retired phase doc)

> **✅ DONE** (2026-07-08; started 2026-07-07, recon wf_9944fe61-fcc). All 7 exit criteria hold (see the reconciled
> list above). Steps 1–7 DONE; step 8 SUPERSEDED by the bind-time gating migration; steps 9–11 DONE. Battery at
> close: **greenfield conformance 2055 · unit 227 · characterization 32 · FULL legacy guard NIST 353 MATCH** (0
> regressions). Landing commits pushed to main: P2.1 `62e09db1` → P2.11 (this close).
> > **Landed (this session reordered the doc's steps 3↔4 — adapter first, then move):**
> > - P2.1 (62e09db1, DEVLOG 670) — the `Cobol.Net.Editions` leaf assembly + refs + `EditionInfo`.
> > - P2.2 (26edb4fa, DEVLOG 671) — `EditionSeverity` + `IDiagnosticSink`/`EditionDiagnostic`.
> > - P2.3 = doc **step 4** (7eae4715, DEVLOG 672) — `EditionContext` adapter (implements `IDiagnosticSink`, backed by `EditionInfo`).
> > - P2.4 = doc **step 3** (960ee15d, DEVLOG 673) — moved registry/reserved-words/codes into Editions; `Check` is `(EditionInfo, IDiagnosticSink, id, where)` sink-based (byte-identical); `EditionSeverityPolicy` added; 53 Check sites updated.
> > - P2.5 (b737ab7c, DEVLOG 674) — `constructs.json` is the single source; `ConstructRegistry.g.cs`/`Constructs.g.cs`/`Gating/GateId.cs` generated by `scripts/gen-constructs.ps1` (committed, mirrors the reserved-words discipline — NOT MSBuild); drift test guards all fields + Constructs/GateId coverage.
> > - P2.6a (a02e6243, DEVLOG 675) — `Constructs.*` compile-checked id-consts at all 54 call sites.
> > - P2.6b (3d707f24, DEVLOG 676) — folded the 5 inline gates into registry rows (0803/0810/0811/0882 active; 0816 END-ACCEPT folded-but-**pending** because the grammar has no `END_ACCEPT` — the gate is unreachable dead code, a documented discovery). 84 active / 11 pending constructs.json rows.
> > - **P2.7 (DEVLOG 679) — the parse-layer COBOLNET0900 flows through the ONE `ConstructRegistry.Check` funnel (forward stamping TRIED, adversarially DISPROVED, R1-mitigation LANDED).** `CobolParserCoreBase` single-sources the dialect year via `EditionInfo Edition` (`DialectLevel` is a shim; `is85/2002/2014/2023` delegate to `Edition.Has`) — P5. `EditionGateHints` is thinned to a pure signature→`Constructs.*` row-id recognizer (the hand-copied `Display`/`IntroducedIn`/`Citation` metadata DELETED — P1/P7); `CobolErrorStrategy` renders the 0900 message via `ConstructRegistry.Check(edition, sink, id, where)` (display/edition/citation are the registry row's), JSON/XML → `COBOL0313` directly.
> >
> > **Steps 8–11 as-built (the completion):**
> > - **Step 8 (delete `EditionGateHints`) SUPERSEDED by the bind-time gating migration (DEVLOG 680–690).** Forward
> >   `{Gate}?` predicate stamping was adversarially disproved (wf_b73eff97 — ANTLR evaluates hoisted predicates
> >   SPECULATIVELY at the stuck token, so typos like `IF W = .` / a stray `)` / `SUPPRESS` got a confidently-wrong
> >   0900; risk R1, DEVLOG 679). Rather than a stamp, the migration moved every HARD-reserved construct's
> >   introduction gate to a bind-time `ConstructRegistry.Check` at its recognition point (the step-6 pattern),
> >   removing both the parse predicates and the reverse signatures for those constructs. What remains is the
> >   irreducible reservation-word residue (XOR / boolean ops / SHARING / RETRY / UNLOCK / PROPERTY — user words
> >   below their edition, whose parse predicate is load-bearing), diagnosed by the thinned recognizer, renamed
> >   `ReservedWordEditionHints` (DEVLOG 693). The duplicated metadata is gone; the parse-layer 0900 renders through
> >   the ONE `ConstructRegistry.Check`. (This satisfies exit criterion 3's INTENT — reconciled above.)
> > - **Step 9 DONE (P2.9, DEVLOG 692):** the frontend preprocessor gates (`ReferenceFormatProcessor.EditionGates`,
> >   `CopyProcessor.OnNonPseudoTextOperand`) consume the ONE `EditionSeverityPolicy` — no local `if(permissive)`.
> > - **Step 10 DONE (P2.10a/b, DEVLOG 694–695):** the first-class `DiagnosticDescriptor` registry
> >   (`Cobol.Net.Editions/Diagnostics/DiagnosticCatalog`) — the edition band single-sourced from `EditionCodes`,
> >   the `COBOLNET0899` catch-all split into ~40 addressable per-feature/per-rule descriptors (deferrals vs
> >   validations, byte-stable code), the reused `COBOLNET1533` split into 3 by ISO §; generated `docs/DIAGNOSTICS.md`
> >   + `DiagnosticRegistryDriftTests` (unique Ids · no bare split-code literal · doc in-sync).
> > - **Step 11 DONE (P2.11, this close):** DOC_INDEX row, `DESIGN-edition-framework.md` banner + resolved Qs,
> >   `resume-prompt.md` STATE → P3, this STATUS → DONE.
> > - **P1/P7 (metadata duplication) FIXED; P3 (eliminate the signatures via a forward gate) is NOT achievable** — R1
> >   disproved it; the bind-time migration is the correct realization instead. Do not re-attempt stamp-as-identifier
> >   (kept as a warning).
> 
> > The executing session MUST update this line as it works: `IN PROGRESS @ step N` while working, then
> > `DONE` when every exit criterion holds. Record the DEVLOG entry numbers you allocate next to each commit.
> 
> ---


### PHASE-03 record (absorbed from the retired phase doc)

> > **Phase 3 delivers the version-conformance pipeline** (design:
> > [`docs/rearchitecture/DESIGN-version-conformance-pipeline.md`](DESIGN-version-conformance-pipeline.md), authoritative
> > for the gating mechanism) — ONE mechanism: **superset parse** (no edition `{isXXXX()}?` gates; a committed-match
> > construct-id annotation stamps identity, numbers stay in `constructs.json`) → **edition-agnostic bind** (zero
> > `ConstructRegistry.Check` calls; bound nodes carry `.Syntax`) → **one `VersionConformancePass`** over the bound tree
> > as the sole gate → **emit-if-clean** (bind/emit split so codegen never runs on an errored tree); there is no
> > `ReservedWordEditionHints`. Built RESIDUE-FIRST. The step notes below cover the harness-driven VCR audit + the
> > editions-framework wiring on the P2 primitives; **Steps 12–15** carry the remaining pipeline delivery (Batch C
> > residue → recogniser deletion → pass skeleton → close).
> 
> `✅ DONE (2026-07-10) — ALL 15 steps complete; the version-conformance pipeline is LIVE and all 9 exit criteria hold (reconciled below). The two-arm VersionConformancePass is the SOLE edition gate; the binder is edition-agnostic save the one documented UDF exception.` (Steps 12–13 DONE — Batch C migrated + the recogniser deleted, DEVLOG 715–717.) **Sub-step ledger:** 14g.1 = 8 PicInfo USAGE gates → bound-arm (DEVLOG 732); 14g.2 = the 4 data-description-clause gates → parse-arm (TYPEDEF reclassified bound→parse by the adversarial review, DEVLOG 733–734); 14g.3 = OO class/interface + OCCURS DYNAMIC → parse-arm (+ report/screen shared-rule fix, DEVLOG 735–736); 14g.4 = file-control SHARING/LOCK-MODE + SPECIAL-NAMES FOR + PD RETURNING/RAISING → parse-arm (SHARING/LOCK-MODE reclassified bound→parse; PD RETURNING/RAISING InMethodDefinition-guarded, DEVLOG 737–738); 14g.5 = FUNCTION-PROTOTYPE → bound-arm + REPOSITORY → parse-arm + PICTURE skeletons (E/national-edited) → bound-arm via PicInfo.SkeletonGate + dead SkeletonUsage/NotImplementedSkeleton deleted, + the SUM-counter drop review-fix (DEVLOG 739–740).
> 
> **⛔ PHASE-03 CLOSE — exit-criteria reconciliation (2026-07-10, all 9 HOLD):** (1) standing continuity sweep — `VersionMatrixTests.Cobol85Program_StillCompilesAtLaterEdition` (`[Theory][MemberData(ContinuityCells)]`) ✅; (2) every gate has a negative witness — all 99 `constructs.json` rows carry a `source` fixture verified by the matrix theories ✅; (3) VCR generated + drift-guarded — `gen-vcr.ps1` + `VcrDriftTests` ✅ (DEVLOG 701); (4) INV-1-strong seeded + guarded — 349/349 byte-exact @ 2023 permissive, the `inv1-strong-2023` CI job ✅; (5) FULL legacy guard 353 MATCH + greenfield battery green ✅; (6) validator/registry/gates on `EditionInfo`+`IDiagnosticSink` — `VersionConformancePass` holds `_edition`/`_sink` ✅; (7) all 7 residue gates migrated + `ReservedWordEditionHints` deleted ✅ (DEVLOG 709–717); (8) the pass is the SOLE funnel + the binder is edition-agnostic — grep-assert: the only `ConstructRegistry.Check` outside the pass is `StatementBinder.Udf.cs:63` (the documented UDF exception — an intrinsic vs user-function call is syntactically identical) ✅; (9) emit unreachable on errors — `CompilerDriver.cs:118` returns `BindError` before `Emit` when `Diagnostics.Count > 0` ✅. **Battery at close: greenfield conformance 3157 · unit 223 · characterization 32 (byte-exact throughout 14g — the 14g.6 re-baseline was a NO-OP) · INV-1-strong 349/349 · FULL legacy guard NIST 353 MATCH / 0 regressions.** Carried-forward (NOT a P3 blocker): the flagged latent OO-env double/zero-bind (DEVLOG 738 — class-level env-division config mis-registration; a dedicated OO-env fix, likely in the PHASE-09 OO rearchitecture). **NEXT: PHASE 04 — frontend consolidation.**
> **⛔ Step 14h DONE (DEVLOG 725–729) — the parse-arm two-arm pass is built and every SYNTACTIC gate migrated.** The
> DEVLOG-724 root-cause fix landed as five byte-identical (well-formed) / strict-superset (malformed) sub-commits:
> **14h.1** (`b3f38cf6`, DEVLOG 725) — `EditionValidator` ABSORBED into `VersionConformancePass` as a nested `ParseArm`
> visitor + run POST-bind + the driver's pre-bind fail-fast DELETED (single post-pass `HasErrors` gate) + `EditionValidator.cs`
> deleted; **14h.2** (`fca04674`, 726) — the 6 self-identifying statement gates (UNLOCK/FREE/ALTER/DELETE-FILE/SET-ADDRESS
> + ALLOCATE); **14h.3** (`8b8c7d54`, 727) — the 6 phrase gates (OPEN-SHARING/GOBACK-RETURNING/CALL-BY-VALUE/
> CALL-ON-OVERFLOW/STOP-STATUS/END-ACCEPT); **14h.4a** (`6287791f`, 728) — the 5 clean expression/phrase gates (XOR/
> bare-GOTO/ROUNDED-MODE/RETRY/record-lock-verb); **14h.4b** (`dd227460`, 729) — the boolean-operator + national/boolean
> LITERAL gates (altitude detection + proc-div scoping). The binder now holds ZERO statement/expression edition Checks;
> 7 gates that need a resolved semantic fact STAY bound-arm (SET-object-ref, SET-ptr-UP/DOWN, INVOKE, READ-PREVIOUS,
> START-FIRST/LAST, START-WITH-LENGTH, READ-ADVANCING-ON-LOCK), and UDF stays a documented bind-time exception (an
> intrinsic FUNCTION vs a user-function call are syntactically identical — repository resolution required). Battery at
> close: conformance **3114** · unit **227** · characterization **32** · INV-1-strong @ 2023 permissive **349/349** ·
> FULL legacy guard **353 MATCH / 0 regressions** · Release warnings-as-errors clean. Recon `wf_be806171-b25`. NO `.Syntax`
> back-ref was added — the `BoundTree.cs` invariant stands (the design's §2.2/§6 provisions realized as a two-arm pass).
> 
> **(historical — the CI-RED FINDING that reshaped 14h; now RESOLVED by 14h.1–14h.4b above)
> ⚠ CI-RED FIX (DEVLOG 724): INTRODUCTION/removal gates fire on the construct's
> RECOGNITION (presence), NOT its bound node** — the bound-arm silently DROPS the 0900 whenever the construct binds to
> `BoundUnsupported`/`BoundNop` on a semantic error (below-edition + malformed). CI caught 2 (`ALLOCATE`, `UDF`); the same
> latent flaw affects the OTHER relocated intro/removal STATEMENT gates (UNLOCK-of-undeclared-file, OPEN-SHARING-of-undeclared,
> INVOKE, ALTER, DELETE FILE, SET-*, keyed-phrases, …), untested. **Immediate: `Allocate2002` + `UserFunctionInvocation2002`
> reverted to bind-time Check (byte-identical, CI green).** **14h ROOT-CAUSE FIX (revised): move ALL introduction/removal gates
> to the presence-based POST-BIND PARSE-ARM** (a parse-tree walk over `BoundRunUnit.Tree`, after bind — fires 0900 on syntactic
> presence, semantic errors also accumulate); only genuinely-semantic gates (MOVE-category, attribute phrases that ARE the
> construct) stay bound-arm. ALSO: **verify against a FRESH `dotnet build CobolSharp.sln` before `dotnet test --no-build`** —
> a stale test-bin compiler hid these locally.
> **Step 14e DONE (DEVLOG 722):** END-ACCEPT + INVOKE. **Step 14f DONE (DEVLOG 723):** MOVE-category (`GateMove`
> re-derives the SR5 classification) + UDF-invocation (hoisted `BoundCallProgram.IsFunction`) — the two genuinely-semantic
> statement gates; this completes the bound-arm STATEMENT-gate relocation. Remaining binder edition Checks: the DATA/PIC/OO
> gates (14g, bound-arm) + the SYNTACTIC token/phrase gates (boolean/XOR/national-boolean-literals/bare-GOTO/ROUNDED-MODE/
> record-lock/RETRY, deferred to the parse-arm 14h with EditionValidator).
> **Step 14c DONE (DEVLOG 720):** 7 attribute-conditioned statement gates relocated to the pass (OPEN SHARING, CALL BY
> VALUE, GOBACK RETURNING, READ PREVIOUS, READ ADVANCING-ON-LOCK, START FIRST/LAST, START WITH LENGTH). **Step 14d DONE
> (DEVLOG 721):** two flag-backed statement gates — CALL ON OVERFLOW (`BoundCallProgram.UsedOverflowSpelling`) + STOP…STATUS
> (`BoundStop.HasStatusPhrase`). Statement-gate relocation remaining: END-ACCEPT, INVOKE, ROUNDED MODE IS, sequential
> record-lock, RETRY (flag/multi-site, 14e) + boolean/XOR/national-boolean-literals/MOVE-category/bare-GOTO/UDF
> (expression-descent, 14f). Battery each: conformance 3114 · characterization 32 · unit 227 · FULL legacy guard 353 MATCH.
> **Step 14 lands as ordered byte-identical sub-commits 14a–14f** (decision-complete recon `wf_edbcd62a-d8a`; the pass is a
> TWO-ARM pass — a bound-tree arm identifying gates by node-type/semantic-attribute + a retained parse-tree arm for the
> EditionValidator-origin syntactic + §8.9 gates; NO `.Syntax` back-ref added to any bound node — the `BoundTree.cs` invariant
> stands, within the design's §2.2/§6 provisions). **Step 14a DONE (DEVLOG 718):** the bind/emit split — `CSharpEmitter.Emit`
> → `Bind(tree,edition,turnEvents) → BoundRunUnit` + `Emit(BoundRunUnit)`; driver runs `bind → pass → HasErrors gate →
> CheckOnly? : emit`; codegen never runs on an errored tree (exit 9). **Step 14b DONE (DEVLOG 719):** `VersionConformancePass`
> stood up (the bound-tree arm + a COMPLETE nested-statement walker) + the first 8 self-identifying-node statement gates
> relocated binder→pass (UNLOCK/ALLOCATE/FREE/SET-object-ref/SET-ptr-UP-DOWN/ALTER/DELETE-FILE/SET-ADDRESS); the pass runs in
> BOTH full-compile and CheckOnly. Battery at 14b: edition/matrix tripwires 1804 · conformance 3114 · unit 227 ·
> characterization 32 · FULL legacy guard 353 MATCH. Step 2 done (DEVLOG 698): `EditionValidator` re-homed onto
> `EditionInfo` + `IDiagnosticSink`. **Step 6 Tier-2 done (DEVLOG 699):** the VCR narrative was spec-audited by a
> 13-agent workflow (`wf_acc42f62-8a4`) — 133 rows, **125 accurate / 3 divergent / 5 unverifiable**; the 3
> divergent (rows 54/68/69, all "Old"-behavior prose) fixed against quoted spec lines; the 5 unverifiable are the
> FLAG-02 rows whose flagging rule + § the audit confirmed (only the 2002 behavior is out-of-spec, matching their
> existing caveat).
> 
> **⛔ Step 6 DESIGN REFINEMENTS (recorded from the build recon — supersede the Step 6 prose below where they conflict):**
> 1. **Coverage is FORWARD only.** Recon found only **15 of 95 constructs are named in any VCR row** — the VCR
>    deliberately does not narrate most 85→2002 / 2002→2014 introductions (its own documented scope limit). So the
>    Tier-1 coverage check is: *every `<!-- gate:id -->` anchor resolves to a real construct* (forward). A "every
>    gated construct has a VCR row" biconditional is NOT achievable/desired today; it grows only as Step 5 backfills
>    rows. There is **no `vcrRow` field on `constructs.json`** — the link lives in the VCR anchor (VCR→construct).
> 2. **No separate `VcrStatusEmitter` / `vcr-status.json`.** The plan's emitter would re-run the exact
>    (construct × edition) cells the matrix theories (`Construct_MatchesEditionExpectation` etc.) already gate — a
>    redundant ~380-compile path. The derived status is instead the `constructs.json` `status` flag
>    (`active`→"done" / `pending`→"pending"), which the matrix ALREADY makes fixture-verified (a row is `active`
>    only when its cells pass, CI-enforced). `gen-vcr.ps1` renders that flag; for a row anchoring multiple
>    constructs it ANDs them. This keeps the singular pattern (one accept/reject gate = the matrix) instead of a
>    parallel recompute engine.
> 3. **Step 3 `expectDiagnostic` enrichment is already adequate** — the 3 dual-window rows that need
>    `expectDiagnosticBelow` already have it; pure-intro rows use the `expectDiagnostic` fallback (matrix line 97).
>    The remaining Step-3 field is `variant` (Step 7 only).
> 
> **⛔ Step 6 DONE (DEVLOG 701).** The VCR transform landed: the hand `Status` column is removed from all 147
> change-table rows and replaced by a machine anchor in the gating cell (`<!-- gate:id -->` ×17 / `todo` ×103 /
> `ref-only` ×12 / `pin-to-spec` ×3, seeded from the old Status cells; per-table cell-count verified consistent).
> The generated "Gating status index" block (`<!-- GEN:VCR-STATUS START/END -->`, 15 anchored constructs) is
> rendered by `VcrDriftTests` (write mode via `scripts/gen-vcr.ps1` → `COBOLNET_WRITE_VCR=1`; the DIAGNOSTICS.md
> pattern). `VcrDriftTests` (4 facts, CI-gated): forward coverage (every `gate:id` → a real construct),
> citation-resolves (every `§clause` in the document is a real clause AND the appendix's quoted fragment is
> inside it — the `cite.py --check` contract), no-line-numbers (a spec LINE ref may never return), and
> index-in-sync. Tier-2 (the narrative
> audit) is DONE (DEVLOG 699). Battery: conformance 2058 · unit 227 · guard 353 MATCH.
> 
> **⛔ Step 5 DONE (DEVLOG 702).** Recon found the matrix was already near-complete: all 69 binder-`Check` ids are
> `constructs.json` rows by construction, and the 34 grammar `{isXXXX()}?` predicates map to existing rows EXCEPT
> two residue introduction gates that lacked a row — added as `pending` (verify-by-running set the metadata):
> `procedure-raising-2002` (PROCEDURE DIVISION RAISING — binds at 2002+ / a level-3-EC-USER SR 0858, but below 2002
> yields a generic `COBOL0001`; the clean-0900 gate is Step-10 loud-hole work) and `inline-method-invocation-2023`
> (`identifier(args)` — no distinctive token, OO-wave-owned). Registry regenerated (97 rows / 57 GateId members).
> There are **zero `{is2014()}?` grammar gates** (2014 features are bind-gated with rows). The broad 85→2002 VCR
> *narrative* (Table 7) growth is blocked on the ISO-2002 standard (not in the repo; the VCR's own scope note) —
> left as incremental "grow as researched," NOT fabricated.
> 
> **⛔ Step 8 DONE (DEVLOG 703).** INV-1 continuity is now a STANDING IN-PROCESS gate: extended the existing
> `VersionMatrixTests.Cobol85Program_StillCompilesAtLaterEdition` from a 13-row `[InlineData]` seed to the FULL
> witness set via `[Theory][MemberData(ContinuityCells)]` (corpus green∪divergent × {2002,2014,2023}, **1047 cells**,
> CheckOnly + xUnit-parallel, ~24s) — permissive-compiles + strict-failures-carry-a-band-code, in the SAME mechanism
> as `NistProgram_MatchesGolden` (NOT a new `[Fact]`/`Parallel.ForEach` class — an initial such class was reverted,
> per an owner correction: integrate into the existing mechanism / migrate the seed, don't fork a parallel one).
> INV-1-strong (exit 4) is the EXISTING env-parameterized `NistDifferentialTests` (verified locally: **349/349
> byte-exact at `--std 2023 --permissive`**), now guarded per-CI by repurposing the `inv1-sweep` job →
> `inv1-strong-2023` (the redundant bash continuity CI step retired — the in-process test is authoritative;
> `version-continuity-sweep.sh` kept as a CLI convenience). Battery: conformance **3092** · unit 227 · guard 353 MATCH.
> 
> **⛔ Step 7 DONE (DEVLOG 704).** The INV-3 behavior-variant matrix is stood up as a LOUD discovery tool:
> `VersionBehaviorMatrixTests` — `[Theory][MemberData]` over `constructs.json` rows carrying a `variant` block ×
> their valid editions (SAME mechanism + catalogue as `VersionMatrixTests`, per the corrected singular-pattern
> principle) — runs each case at every edition (must compile+run via the new `EditionHarness.CompileAndRun`),
> diffing stdout for `confirmed` variants and cataloguing `pending` candidates loud (a `confirm` note). Seeded ONE
> cited pending candidate (`arithmetic-intermediate-precision-2023`, E.2 item 6 / VCR row 12) — no confirmed
> edition-variant behavior exists yet (DEVLOG 517; the one scaled-integer pipeline makes arithmetic
> edition-invariant), so the matrix is a discovery surface, not a gate. Battery: conformance **3097** · unit 227 ·
> guard 353 MATCH.
> 
> **⛔ Step 9 DONE (DEVLOG 705).** Like Steps 1/4, already built by prior work: `CorpusRunnerTests` IS the discovery
> runner — `Manifest_CoversEveryProgram_NoOverlap` (integrity: nothing silently undiscovered),
> `EnabledProgram_CompilesStrict_AndMatchesOutIfPresent` (the per-edition positive runner), and
> `EnabledNegativeCase_RejectsWithItsDiagnostic` (the negative runner, `*> reject-at:` header). The corpus is fully
> enabled (2002: 78 · 2014: 17 · 2023: 11 positives · 46 negatives, 0 pending). Per the corrected singular-pattern
> principle I reused it, not forked. Added the ONE named-but-missing 2014 seed — `ARITHMETIC IS STANDARD-DECIMAL`
> (§8.8.1.4; verify-by-running: @2014 `W=2.00000` full-decimal-precision, @2002 rejected `COBOLNET0804`) — via a
> `.cob`+`.out`+manifest entry the runner auto-discovers (`CorpusRunnerTests` 157→158). Battery: conformance 3098 ·
> guard 353 MATCH.
> 
> **⛔ Step 10 DONE (DEVLOG 706–707; owner chose FULL WIRING).** Swept for edition constructs emitting a generic
> `COBOL0001` / silently accepting a newer feature, and wired the two genuine holes loud:
> - **`procedure-raising-2002`** (DEVLOG 706) — RAISING@<2002 generic `COBOL0001` → `COBOLNET0900` via a
>   `ReservedWordEditionHints` arm; row flipped active + a negative fixture.
> - **`sync-on-group-2023`** (DEVLOG 707, owner-chosen disposition) — SYNCHRONIZED on a GROUP item (a 2023
>   introduction, Annex E.3.2 item 6) was silently ACCEPTED below 2023. Now gated in `DataBinder.ResolveIndexItems`
>   via a `DataItem.Synchronized` flag: **error strict / warning-permissive (accept-inert)** — the removed-severity
>   seam, which keeps INV-1 continuity (SYNC is a no-op). Row + negative fixture.
>   ⛔ **SUPERSEDED by CA14 (2026-07-28):** that accept-inert disposition is RETIRED — the site routes through the
>   canonical `Check` funnel and errors on BOTH axes, like every other introduction.
> 
> The un-wireable residue is CATALOGUED (not silently absent): `inline-method-invocation-2023` (pending row, Step 5 —
> `identifier(args)` has no distinctive token, OO-wave-owned) and NO SIGN of PACKED-DECIMAL (a 2023 feature not in the
> grammar → a `COBOL0307`, needs grammar work to gate cleanly; noted for the M4/2023 feature wave). Battery:
> conformance 3108 · unit 227 · guard 353 MATCH.
> 
> **✅ RESIDUE-FIRST MIGRATION COMPLETE — all 7 gates migrated + the recogniser DELETED (DEVLOG 709–717).** Every
> residue gate dropped its grammar edition predicate and now gates bind-time via `ConstructRegistry.Check`:
> **UNLOCK #5** (`2daec9cb`) · **PROPERTY #7** (`7311dfbd`) · **PD-RAISING #6** (`840b0abf`) · **XOR #1** (`d3cdae6c`,
> Batch A) · **SHARING #3** (`1b74f739`, Batch B — OPEN collision byte-safe) · **RETRY #4** (`4efcca71` — 6 grammar
> sites, `retryPhraseAhead()` forward-detect for OPEN, closed a real 4-site bind gap) · **boolean #2** (`3d0ec86e` —
> operator tiers + COMPUTE F2 + the operand-adjacency `boolExprAhead()` ENTRY; DEVLOG-621 surface intact) — Batch C
> complete. **Step 13** (`<pending push>`): `ReservedWordEditionHints` DELETED, the vendor JSON/XML COBOL0313
> disposition relocated to `CobolErrorStrategy`. Each grammar change: FULL legacy guard **353 MATCH, 0 regressions**.
> Battery at head: greenfield conformance **3114** · unit **227** · characterization **32** GREEN.
> 
> **RESUME AT: Step 15 — PHASE-03 CLOSE. ✅ 14g.5 DONE** (DEVLOG 739) — FUNCTION-PROTOTYPE → bound-arm; REPOSITORY → parse-arm; the E/national-edited PICTURE skeletons → bound-arm via `PicInfo.SkeletonGate` (NOT the recon's raw-picture scan — reusing PicInfo's exact detection + GateData's where-strings is drop-proof + covers report items); dead `SkeletonUsage`/`NotImplementedSkeleton` deleted. **The grep-assert PASSES: the sole `ConstructRegistry.Check` outside the pass is the one UDF exception (`StatementBinder.Udf.cs`).** Step 15 = walk the exit-criteria checklist (items 1–9), final doc sweep, STATUS→DONE (14g.6 snapshot re-baseline is a NO-OP — characterization stayed byte-exact throughout 14g). ✅ **Step 14h is DONE** (14h.1–14h.4b,
> DEVLOG 725–729, pushed + CI-green): the two-arm `VersionConformancePass` is built — a PARSE-tree arm (`ParseArm`, the
> absorbed `EditionValidator` running post-bind) owns every SYNTACTIC introduction/removal/phrase/expression/literal gate
> + the §8.9 reserved-word funnel, firing on RECOGNITION (the DEVLOG-724 fix); the driver's pre-bind fail-fast is gone;
> `EditionValidator.cs` is deleted. **14g** moves the remaining ~30 bind-time DATA/PICTURE/OO gates into
> `VersionConformancePass` — but the recon (`wf_0d98d218-087`) proved only ~12 are resolved-fact fits for the BOUND-arm;
> ~19 must go to the PARSE-arm (see the DECISION-COMPLETE PLAN immediately below; refined by DEVLOG 734 — TYPEDEF moved
> bound→parse). ✅ **14g.1 DONE** (8 PicInfo USAGE gates → bound-arm, DEVLOG 732). ✅ **14g.2 DONE** (the 4
> data-description-clause gates → parse-arm — TYPEDEF reclassified bound→parse by the adversarial review, DEVLOG 733–734).
> ✅ **14g.3 DONE** (OO class/interface definition + OCCURS DYNAMIC → parse-arm, DEVLOG 735; class/interface would
> under-count a bound-arm walk, OCCURS DYNAMIC would over-count TYPE clones — recognition is byte-exact). Only UDF stays
> bind-time (documented exception). Then **Step 15 — phase close**: full battery + doc sync + STATUS→DONE + the exit-criteria checklist (items 1–9); re-point
> CheckOnly/EditionHarness/INV-1 legs (already true — all read the one sink; verify); grep-assert zero
> `ConstructRegistry.Check` outside the pass; fold in the Step-12(c) residual (refresh the migrated rows' stale
> `constructs.json` citations + grammar comments).
> 
> #### Step 14g — DECISION-COMPLETE PLAN (recon `wf_0d98d218-087`, 2026-07-09)
> 
> **⚠ RE-SCOPING FINDING (load-bearing):** the invariant to satisfy is *zero `ConstructRegistry.Check` in the binder/
> PicInfo/OoClassTable/OdoModel/emitter except the ONE UDF exception* — satisfied by moving a gate into
> `VersionConformancePass`, NOT necessarily its bound-tree arm. Only genuine **resolved-fact** gates fit the bound-arm
> enumerator; the rest are recognition/syntactic/unreachable → the **parse-arm** (else the DEVLOG-724 drop, a
> TYPEDEF-clone over-count, or "no bound carrier"). The split:
> 
> - **BOUND-arm** (`GateData` enumerator / `Run` unit loop): the 8 PicInfo USAGE gates (National/Boolean/Pointer/
>   ObjRef/BinaryCharFamily/Float×3) [14g.1 DONE]; `FunctionPrototype2002` (`Run` over `CallUnit.IsPrototype`) [14g.5 DONE];
>   `PicExternalFloat2002`/`NationalEdited2002` (via `PicInfo.SkeletonGate`, ⚠ RECLASSIFIED from the recon's parse-arm
>   raw-picture scan — the flag reuses PicInfo's exact detection + GateData's where-strings, drop-proof + covers report
>   items) [14g.5 DONE] — all key on a resolved fact retained after declaration errors. ⚠ `TypedefDef2002` (DEVLOG 734,
>   14g.2) AND `FileSharingClause2002`/`LockModeClause2002` (DEVLOG 737, 14g.4) were RECLASSIFIED to the PARSE-arm — the
>   same finding both times: a bound-arm gate keyed on the resolved fact DROPS the 0900 on a declaration error (the
>   typedef item discarded from the forest; a file discarded on a SELECT error), whereas the binder fired on the clause's
>   PRESENCE. Recognition is drop-proof.
> - **PARSE-arm** (recognition): `BasedClause2002`/`TypeClause2002`/`PropertyClause2002`/`TypedefDef2002` (flags cleared/
>   nulled on error, or — for TYPEDEF — the item discarded from `ConformanceForest` by `RegisterTypeDecl`/method-scope →
>   724 drop; all guarded against a level-66/88 mis-attachment); `ClassDefinition2002`/`InterfaceDefinition2002` (fire before the dedup `continue` → under-count + collision
>   drop); `OccursDynamic2014` (TYPEDEF clones over-count a per-item walk; one `occursClause` = one fire);
>   `SpecialNamesForNational2002` ×3 (FOR token) [14g.4]; `FileSharingClause2002`/`LockModeClause2002` (clause presence;
>   reclassified from bound) [14g.4]; `ProcedureReturning2002`/`ProcedureRaising2002` (`InMethodDefinition`-guarded — the
>   procedureDivision rule is shared with method PDs) [14g.4]; `RepositoryProperty/Interface/Class2002` (config specifiers, name-embedding where) [14g.5 DONE].
>   (`PicExternalFloat2002`/`NationalEdited2002` were planned here as a raw-picture scan but RECLASSIFIED to the BOUND-arm
>   via `PicInfo.SkeletonGate` — see above; 14g.5 DONE.)
> 
> **Enumerator:** add public `DataBinder.ConformanceForest()` = `Roots.Concat(LinkageRoots).SelectMany(Walk).Concat(
> TypeDecls.Values.SelectMany(Walk))` — ⚠ `LinkageRoots` + `TypeDecls` are OFF `Roots` (the existing private `AllItems()`
> misses them). `Run` adds `GateData`/`GateFiles`/`GateReports` + `if (unit.IsPrototype) Check(FunctionPrototype2002,…)`
> inside the existing `group.Units`/`group.Classes` loop. **Dedup:** gate PER-DataItem on the resolved attribute,
> EXCLUDING TYPE-expansion clones + compiler temps (they share the template `PicInfo` by reference); NEVER dedup by
> `PicInfo` identity (`PicInfo.PointerItem` is a static singleton → would collapse all pointers to one fire). Key USAGE on
> `(OwnUsage, Pic.Category, Pic.Usage)` — `OwnUsage` mandatory (group headers shed `Pic`). **Where-strings scope-aware:**
> main `"data item '{CobolName ?? "FILLER"}'`" (hard-coded in `GateData`); report `"RD '{model.Name}' printable item
> '{…}'`" (`GateReports` over `unit.Data.Reports`); file/typedef/prototype constant.
> 
> **Sub-commits:** **14g.1 ✅ DONE (DEVLOG 732, `bf6fc5b5`)** — `DataBinder.ConformanceForest()` + `GateData` + the 8
> PicInfo USAGE gates; `LoudGuardTests` rewritten off its direct `PicInfo.ParseUsage`/`Analyze` gate asserts; 8
> `UsageDataEditionTests` exact-count witnesses (one 0900/item; two distinct pointers → two 0900s; TYPEDEF member
> referenced twice → gated once). Battery: conformance 3122 · unit 223 · characterization 32 · INV-1 349/349 · guard
> 353 MATCH. (⚠ the conformance suite is contains-based, blind to over-count — hence the exact-count witnesses.)
> **14g.2 ✅ DONE (DEVLOG 733–734)** — the 4 data-description-clause gates, ALL to the parse-arm (recognition): 4 `ParseArm`
> overrides (`VisitBasedClause`/`VisitTypeClause`/`VisitPropertyClause`/`VisitTypedefClause`), each guarded by
> `InConditionOrRenamesEntry` (skip level-66/88, mirroring the binder's pre-clause-loop skip); the PROPERTY storage-loop
> branch deleted; `GateData` stays USAGE-only. ⚠ **DESIGN CORRECTION (DEVLOG 734, adversarial review of `2efa4ea`):** the
> plan put TYPEDEF in the BOUND-arm ("init-only `IsTypedef` survives"), but the typedef ITEM is DISCARDED from
> `ConformanceForest` when `RegisterTypeDecl` rejects it (unnamed/FILLER `return`, duplicate-name `TryAdd` fail) or it
> binds into method `LocalRoots`/`StaticRoots` — so the bound-arm dropped the 0900 on those paths (the DEVLOG-724 class,
> 3 confirmed defects: FILLER-typedef under-fire, duplicate-typedef under-fire, level-88-clause over-fire). TYPEDEF needs
> no resolved fact → recognition is the correct home; corrected + 3 regression witnesses. New `DataClauseEditionTests`
> (9 exact-count witnesses); byte-neutral (characterization 32 byte-exact — the content-SORTED diag surface is blind to
> the arm change; `char_neg_typedef85` unchanged, NO re-baseline). Battery: conformance 3131 · unit 223 ·
> characterization 32 · INV-1 349/349 · guard 353 MATCH. **14g.3 ✅ DONE (DEVLOG 735–736)** — OO class/interface
> definition (`ParseArm.VisitClass/InterfaceDefinition`, name-embedding where-strings, `Build` called once → byte-exact
> count) + OCCURS DYNAMIC (`VisitOccursClause` on the DYNAMIC alternative), all parse-arm; new `OoOccursDynEditionTests`
> ×8 (incl. the over-count witness + 2 review regressions). ⚠ **SHARED-RULE HAZARD (DEVLOG 736, adversarial review of
> `729b6c4f`):** `occursClause` is shared by data / report-writer / screen grammars, but the former `OdoBindOccursSpec`
> gate reached only data (`BindEntry`) — so a bare tree walk OVER-fired OCCURS DYNAMIC in report groups (extra 0900) and
> screen sections (compiles→rejects). The NEGATIVE level-66/88 guard fired-by-default with no enclosing entry; replaced
> by a POSITIVE `InGatedDataEntry` (fire only inside a non-66/88 `dataDescriptionEntry`) reused by all five data-clause
> gates. **Lesson for 14g.4/14g.5: before a parse-arm gate, check whether its grammar rule is SHARED across sections and
> scope the visitor to the binder's actual reach.** Battery: conformance 3139 · unit 223 · characterization 32 · INV-1
> 349/349 · guard 353 MATCH. **14g.4 ✅ DONE (DEVLOG 737)** — the 7 config/file-control/PD-header gates, ALL parse-arm:
> `VisitSharingClause`/`VisitLockModeClause` (⚠ SHARING/LOCK-MODE reclassified bound→parse — the same inverted-rationale
> correction as TYPEDEF: a bound-arm FileModel gate drops the 0900 on a SELECT error; recognition is drop-proof) +
> 3× SPECIAL-NAMES FOR (`VisitAlphabet/ClassDefinition/SymbolicCharactersClause`, on the `FOR` token) + PD RETURNING/RAISING
> (`VisitReturning/RaisingClause`, `InMethodDefinition`-guarded — the procedureDivision rule is SHARED with method PDs but
> `CallBindLinkage` gated program units only; the 14g.3 lesson applied in recon). Scope-union verified: every program
> (incl. nested), class, and factory has its own DataBinder whose BindDeclarations runs the three binders, so the
> whole-tree walk = the union. New `ConfigPdEditionTests` ×10 (incl. `MethodReturning_At85_NotGated`). ⚠ **REVIEW (DEVLOG
> 738, adversarial `81dd6c37`):** the CLASS-scope env-clause gates now fire the SPEC-CORRECT 1× (the former bind-time
> gates fired 2×/0× — `OoReparent{Class,Factory}Data` bind the class-level env via the singular accessor 0/1/2×); the
> parse-arm is MORE correct (kept + 2 witnesses pinned); byte-neutral for program-unit scopes; verdict unchanged. **FLAGGED
> LATENT BUG for a dedicated OO-env fix:** that same double/zero-bind mis-registers class-level CURRENCY/ALPHABET/SELECT
> (shadow-0× SILENTLY IGNORES class-level config in valid 2002+ classes) — bind class-level env ONCE, visible to both
> halves. Battery: conformance 3149 · unit 223 · characterization 32 · INV-1 349/349 · guard 353 MATCH. **14g.5 ✅ DONE
> (DEVLOG 739)** — FUNCTION-PROTOTYPE → BOUND-arm (`Run` over `CallUnit.IsPrototype`) + REPOSITORY CLASS/INTERFACE/PROPERTY
> → parse-arm (`VisitRepositoryEntry`, name-embedding where) + the E/national-edited PICTURE skeletons → BOUND-arm via a
> new `PicInfo.SkeletonGate` flag (⚠ NOT the recon's parse-arm raw-picture scan — that would re-implement PicInfo's
> expansion + GateData's where-strings; the flag reuses both, drop-proof, covers report items). `NotImplementedSkeleton`
> split → `StagedNotImplemented` (0899); the dead `SkeletonUsage`/`NotImplementedSkeleton` DELETED (dead since 14g.1). New
> `RepositoryPrototypeEditionTests` ×7; `LoudGuardTests` external-float leg rewritten to the SkeletonGate carrier.
> **⛔ grep-assert PASSES: the sole `ConstructRegistry.Check` outside the pass is `StatementBinder.Udf.cs:63` (the UDF
> exception).** ⚠ REVIEW-FIX (DEVLOG 740, adversarial `6dd27247`): the report SUM-counter external-float/national-edited
> 0900 was DROPPED below 2002 — the SUM-counter scale-derivation `Analyze` (a THIRD Analyze site) discards its PicInfo, off
> GateData's forest + printable-item walks. Fixed: `ReportSumModel.SkeletonGate`/`SkeletonWhere` + a GateData `report.Sums`
> walk (byte-exact to the two former inline sites); regression witness added. Battery: conformance 3157 · unit 223 ·
> characterization 32 · INV-1 349/349 · guard 353 MATCH. —
> **RESUME AT Step 15 (phase close):** exit-criteria checklist 1–9; 14g.6 snapshot re-baseline is a NO-OP (characterization
> byte-exact throughout 14g). Guard per commit: fresh `CobolSharp.sln` build → greenfield conformance+unit+characterization
> + INV-1 + FULL legacy guard. Full synthesis is in the recon transcript.
> 
> > The executing session updates this line to `IN PROGRESS @ step N` after each step and `DONE` at phase end.
> > Resumption protocol: read this STATUS line, run **Step 0** (battery baseline + AS-BUILT reconciliation) to
> > re-establish ground truth, then continue at the first step whose commit is not yet in `git log`.
> 
> ### Step-0 AS-BUILT reconciliation (2026-07-08 — ⚠ this phase doc was authored BEFORE P2 executed; P2 + the bind-time gating migration already landed several P3 steps)
> 
> **Baseline (green, inherited from the PHASE-02 close, commit `61248d88`):** greenfield conformance **2055** ·
> unit **227** · characterization **32**; FULL legacy guard **NIST 353 MATCH**. `scripts/guard.sh`,
> `scripts/version-continuity-sweep.sh` present. `docs/VERSION_CHANGE_REFERENCE.md` carries **117 `TODO` rows**.
> 
> | Step | AS-BUILT | Evidence | Remaining P3 action |
> |---|---|---|---|
> | **0** baseline + recon | ✅ done | this table | — |
> | **1** P2 framework surface | ✅ present | `EditionInfo` / `IDiagnosticSink` / `EditionSeverity(Policy)` / `EditionDiagnostic` / sink-based `ConstructRegistry.Check(EditionInfo, IDiagnosticSink, id, where)` / `Constructs.g.cs` all exist (P2.1–P2.6) | — |
> | **2** re-home `EditionValidator` → `EditionInfo`+`IDiagnosticSink` | ✅ **DONE** (DEVLOG 698) | ctor now `EditionValidator(EditionInfo, IDiagnosticSink)`; the 2 `VisitCobolWord` direct writes emit via `_sink.Report(new EditionDiagnostic(…))` through `EditionSeverityPolicy`; `_edition.Year`; driver hook `new EditionValidator(edition.Edition, edition)`; `using CobolNet.Binding` dropped. No `EditionContext` on the edition path (exit criterion 6). Byte-identical (guard 353 MATCH; edition/reserved-word/move corpora green). | — |
> | **3** enrich `constructs.json` | ◑ **PARTIAL** | 95 rows carry `id/description/display/diagnosticCode/citation/introducedIn/removedIn/vcr/source/status`; **missing** `expectDiagnostic`(+`Below`) on most rows, **no `variant` blocks**, some rows `status:"?"` (unset). | add `expectDiagnostic`/`expectDiagnosticBelow` + `variant` + fill `status`. |
> | **4** fold the 5 inline gates | ✅ **DONE in P2.6b** | zero bare `Error("COBOLNET0816/0810/0811/0882/0803")` literals remain; all are registry rows (`0816 end-accept` folded-but-`pending` — grammar has no `END_ACCEPT`). | verify each still binds-after-permissive-warn + add negative witnesses only. |
> | **5** backfill 85→2002 / 2002→2014 rows | ❌ partial | — | add introduction rows from the grammar `{isXXXX()}?` gates + confirmed spec deltas (unconfirmed → `pending`). |
> | **6** mechanize the VCR audit | ❌ **NOT DONE** | 117 hand `TODO` rows; no `gen-vcr.ps1` / `VcrStatusEmitter` / `VcrDriftTests`. | build them (the headline leg). |
> | **7** behavior-variant matrix (INV-3) | ❌ **NOT DONE** | no `variant` blocks; no `VersionBehaviorMatrixTests`. | build as a loud discovery tool (candidates `pending`). |
> | **8** in-process continuity + INV-1-strong | ❌ **NOT DONE** (bash only) | `version-continuity-sweep.sh` exists; no `VersionContinuitySweepTests` / `Inv1StrongGoldenTests`. | build the in-process gates. |
> | **9** discovery runners + 2014 seeds | ❌ **NOT DONE** | no `NegativeCorpusDiscoveryTests` / `PerEditionPositiveCorpusTests`; `tests/conformance/2014/` exists. | build runners + seed positives. |
> | **10** catalogue holes LOUD | ❌ **NOT DONE** | witness: SYNCHRONIZED-on-group emits generic `COBOL0001`. | sweep + gate loud; verify the national/boolean skeleton. |
> | **15** phase close (formerly 11) | ❌ | — | after Steps 12–14 (pipeline delivery): full battery + doc sweep + STATUS=DONE. |
> 
> **Net:** Step 1 ✅ and Step 4 ✅ are already satisfied (P2/P2.6b); Steps 2 & 3 are PARTIAL; Steps 5–10 are the
> substantial NEW work (VCR mechanization + three new test surfaces + row backfill + hole cataloguing). Also note
> P2.10 already delivered the `DiagnosticCatalog` registry + `docs/DIAGNOSTICS.md`, which the Step-10 "loud, not
> generic" intent builds on, and `EditionGateHints` is now `ReservedWordEditionHints` (the reservation-word residue
> after the bind-time gating migration — DEVLOG 693). **Exit criterion 6 nuance:** P2 (owner decision Q5) KEPT
> `EditionContext` as the compiler-side collector behind the adapter; P3 satisfies "validator on `EditionInfo` +
> `IDiagnosticSink`" by making the validator depend only on those *types* (the concrete sink passed in may still be
> the `EditionContext` collector, which implements `IDiagnosticSink`) — the validator no longer calls any
> adapter-specific method (`.Error`/`.Removed`/`.DialectLevel`).
> 
> ---


### PHASE-04 record (absorbed from the retired phase doc)

> `DONE (2026-07-10) — Groups A–D complete; all 5 exit criteria hold. The D10 SUBSCRIPT-mode removal was RELOCATED to PHASE 15 (post-G8), so it is no longer this phase's scope.`
> 
> > **✅ PHASE-04 CLOSED (2026-07-10, DEVLOG 748).** Groups A+B+C+D are DONE (A5 743 / B1 744 / C3 745 / D reconciliation 746
> > — the byte-neutral consolidation core: word set single-sourced + drift-guarded; shared DEFAULT/SUBSCRIPT literal
> > fragments; typed `Cst/` façade + 2 anchors migrated; version-conformance leg reconciled). All 5 exit criteria hold
> > (see the reconciled criterion 5). **The D10 owner-override (fully remove the SUBSCRIPT lexer mode + the binder
> > re-parse) has been REMOVED from PHASE 04's scope and RELOCATED to PHASE 15** (see `PHASE-15-…md` §"CUT 2.5") — it is
> > BLOCKED until the frozen legacy compiler (which shares `SUB_*`/`SubscriptEntryContext`) is deleted at PHASE 15 Cut 2,
> > so PHASE 15 (post-legacy-deletion) is the first place it is realistically doable. The D10 DESIGN stays in
> > `DESIGN-frontend-grammar.md §9`; the owner's "fully remove" ruling (master §6 D10) is unchanged — only its schedule
> > moved. This closes PHASE 04.
> <!-- The executing session updates this line to `IN PROGRESS @ step N` and finally `DONE`.
>      Keep a one-line note per completed commit boundary in the "Execution log" at the bottom. -->
> 
> > **(Historical group ledger — PHASE 04 is CLOSED per the STATUS/disposition above; the "RESUME AT" lines below are the
> > as-executed group-by-group progress record, not open work. D10 is now PHASE 15's, not this phase's.)**
> >
> > **✅ GROUP A DONE (A5 landed 2026-07-10, DEVLOG 743).** The word set is single-sourced from
> > `tests/version-matrix/cobol-words.json` (77 rows) → `scripts/gen-cobol-words.ps1` emits `Grammar/Core/CobolWords.g4`
> > (the imported `cobolWord` fragment) + `Parsing/CobolLexerWordSet.g.cs` (the `_dataNameTokens` partial); the hand-written
> > `cobolWord` rule + the lexer `_dataNameTokens` HashSet are deleted; `CobolWordsDriftTests` (×4) binds parser rule +
> > runtime lexer set + reserved-words. Byte-neutral: `.tokens` byte-identical (incl. a cold clean+regen), generated sets ==
> > pre-flip sets (independent re-parse), conformance 3157 · unit 227 · characterization 32 byte-exact · legacy guard **353
> > MATCH / ALL GREEN / 0 regressions**. Adversarial review (wf_16cc83d1-1cc) found + FIXED a false-green drift-guard gap
> > (added the symmetric `subscriptTrigger`-only exact pin; mutation-proven). Reserved-words cross-check DEVIATION recorded
> > (Step A2 item 4). **⛔ RESUME AT: GROUP B** (share SUBSCRIPT/DEFAULT literal token bodies via `fragment` rules; commit
> > boundary B1) — then Group C (the `Cst/` façade), then re-assess Group D against the P3 two-arm pass. NEVER re-introduce
> > an edition predicate.
> >
> > **✅ GROUP B DONE (B1 landed 2026-07-10, DEVLOG 744).** The six DEFAULT/SUBSCRIPT literal token twins now share
> > `fragment` bodies (`STR_BODY`/`NAT_BODY`/`BOOL_BODY`/`INT_BODY`/`DEC_BODY`/`NAME_BODY`) — one definition per shape,
> > referenced by both modes. Byte-neutral: `.tokens` byte-identical, conformance 3157 · unit 227 · characterization 32
> > byte-exact · legacy guard **353 MATCH / ALL GREEN / 0 regressions**; subscript-literal + single-quote probes green.
> > **⛔ RESUME AT: GROUP C** (Step C1 — the typed `Cst/` façade over the ANTLR contexts + migrate the two anchor consumers
> > `ReferenceResolver` and `DataBinder.BindEntry` off raw `GetText()`; commit boundary C3).
> >
> > **(historical — Group-A recon, now executed)** Depends: PHASE 03 ✅ CLOSED
> > (the version-conformance pipeline is LIVE; DEVLOG 741). §1 preconditions ALL PASS: P1 done (generated ns
> > `CobolNet.Frontend.Generated`, dead grammars + JSON/XML removed), P2 done (`Cobol.Net.Editions` present), the proven
> > `gen-reserved-words.ps1` + `ReservedWordsDriftTests` + `reserved-words.json` codegen pattern exists to EXTEND. Neutrality
> > baseline captured: `.tokens` at `/e/tmp/CobolLexer.tokens.baseline` (951 lines). **⚠ Note on Group D:** PHASE 03 built
> > the two-arm `VersionConformancePass` (a parse-arm walking the RAW parse tree + a bound-arm over resolved facts), NOT the
> > design's grammar-action "construct-id annotation side-table" — so Group D's annotation convention is likely SUPERSEDED
> > (the parse-arm reads the tree directly) and the superset grammar is already complete (P3 dropped every edition
> > predicate save the two forward-detects). Re-assess Group D against the AS-BUILT pass when reached; it may reduce to a
> > reconciliation note. Groups A–C proceed as written.
> >
> > **⛔ GROUP-A STEP-A1 RECON RESULT (the deterministic extraction is DONE — do not re-eyeball; the tree may be re-grepped
> > to confirm):** the context-sensitive word set = **77 words** (union of the two current sources). Reconciliation of
> > `nameSlot` (the `cobolWord` rule, `CobolParserCore.g4:25-113`, 71 tokens incl. IDENTIFIER) △ `subscriptTrigger` (the
> > lexer `_dataNameTokens` set, `CobolLexer.g4:30-72`, 76 tokens incl. IDENTIFIER) confirms EXACTLY the doc's predicted
> > asymmetries (FU-1) — capture AS-IS, do NOT "fix" inside the neutral flip:
> > - **70 words in BOTH** → `nameSlot=true, subscriptTrigger=true`.
> > - **`BIT`** → `nameSlot=true, subscriptTrigger=false` (in `cobolWord`, NOT the lexer set — the latent under-trigger).
> > - **`DISPLAY, MERGE, RANDOM, SIGN, SORT, SUM`** → `nameSlot=false, subscriptTrigger=true` (in the lexer set for the
> >   `functionName` collision, NOT `cobolWord`).
> > The full membership is the two cited grammar spans; author `cobol-words.json` (77 rows, sorted by token, each with
> > `token`/`nameSlot`/`subscriptTrigger`/`note`) → `gen-cobol-words.ps1` → `CobolWordsDriftTests` → flip the lexer+parser →
> > verify `.tokens` byte-identical + FULL legacy guard + the name-slot smoke probe → COMMIT A5. (Battery at Phase-04 start:
> > greenfield conformance 3157 · unit 223 · characterization 32 · INV-1-strong 349/349 · legacy guard 353 MATCH.)
> 
> ### Goal (one paragraph)
> The frontend core is a single **superset grammar** — every edition's constructs parse unconditionally, and
> edition legality is decided at bind time by the `VersionConformancePass`
> (`docs/rearchitecture/DESIGN-version-conformance-pipeline.md`); the problem is the **duplication around
> it**. This phase removes two duplications, installs one enabling façade, and completes the grammar side of the
> version-conformance pipeline: (1) the context-sensitive word set
> — the tokens that are keywords in context but legal user-defined words elsewhere — is today hand-synced across
> THREE physically separate places (the lexer `_dataNameTokens` HashSet, the parser `cobolWord` rule, and the
> compiler `ReservedWords` table) with a source comment literally instructing a maintainer to hand-mirror them;
> we make it ONE generated artifact from a declarative `tests/version-matrix/cobol-words.json`, extending the
> proven `gen-reserved-words.ps1` codegen, guarded by a drift test so they provably cannot desync. (2) The
> SUBSCRIPT lexer mode re-declares its own `SUB_*` literal token bodies paralleling the DEFAULT-mode literals;
> we factor the shared bodies into `fragment` rules so each tokenization shape exists once. (3) We introduce a
> `Cst/` typed façade (thin, 1:1-with-grammar-rules wrappers over the generated ANTLR contexts) as the narrow
> cross-assembly surface the binder consumes, and migrate the two highest-churn anchor consumers off raw
> `GetText()` — making a grammar-rule rename a compile error in one file instead of a silent drift across ~339
> `GetText()` sites. (4) We complete the **superset grammar** and install the **committed-match construct-id
> annotation convention** — grammar actions that stamp each recognized edition-gated construct into side-table
> storage keyed by parse context, the grammar-side feed the `VersionConformancePass` reads (per
> `DESIGN-version-conformance-pipeline.md`). **Groups A–C are behavior-neutral; the full battery stays green at
> every commit boundary.**
> 
> ### Exit criteria (all must hold at phase end)
> 1. The context-sensitive word set is **single-sourced** from `tests/version-matrix/cobol-words.json`: the lexer
>    subscript-trigger set and the parser `cobolWord` rule are both generated, and a **`CobolWordsDriftTests`**
>    proves lexer + parser + `reserved-words.json` cannot silently desync (a hand edit to any generated artifact,
>    or a regen that touched only one, fails the test).
> 2. The SUBSCRIPT-mode literal/operator token bodies that have a DEFAULT-mode twin are defined **once** via
>    shared `fragment` rules; the regenerated `CobolLexer.tokens` is byte-identical and the battery is green.
> 3. A `Cst/` typed façade exists for the **highest-churn rules** (`dataReference`, `dataDescriptionEntry`,
>    `cobolWord`, `integerLiteral`) and the two anchor consumers (`ReferenceResolver`, `DataBinder.BindEntry`)
>    read the façade, not raw `GetText()`.
> 4. **Full battery green + snapshots neutral:** greenfield conformance + unit + characterization + the FULL NIST
>    legacy guard (ALL GREEN) + version-matrix accept/reject unchanged across all four `--std` values. The
>    generated `.g.cs` for a representative corpus is byte-identical to pre-phase (behavior neutrality).
> 5. The **superset grammar is complete** for edition GATING — **no edition-REJECTION predicate survives** (every
>    edition-gated construct parses at every `--std`; legality is decided by the `VersionConformancePass`). ⚠
>    **RECONCILED (Group D, DEVLOG 746):** (a) the design's **construct-id annotation side-table** is **SUPERSEDED** —
>    P3 built the two-arm `VersionConformancePass` whose **ParseArm walks the RAW parse tree directly** (no grammar
>    actions, no keyed side-table), so no annotation convention is installed and none is needed. (b) The former claim
>    "the only grammar predicates are the two forward-detects" **UNDERCOUNTED**: the grammar retains a small set of
>    load-bearing **cross-edition DISAMBIGUATION** predicates (NOT rejection gates) — the two forward-detects (the
>    `openClause` `{is2002() || retryPhraseAhead()}?` and the `boolExprAhead()`-based boolean-condition ENTRY) **PLUS**
>    `{is2023()}? inlineMethodInvocationStatement` (`CobolParserCore.g4` — genuinely ambiguous with a subscripted
>    `x(args)` reference), `{is2002()}? linkageProcedureParameter` (`CobolData.g4` — the 2002 procedure-parameter form),
>    and the `{!(is2002() && LA(1)==PROPERTY)}?` VALUE-list negative lookahead (`CobolData.g4` — PROPERTY is a 2002
>    keyword that can follow a VALUE clause). Each resolves a genuine syntactic ambiguity across editions per the
>    design's own "a forward, identity-carrying lookahead survives ONLY where a construct is genuinely ambiguous across
>    editions" allowance; none REJECTS a below-edition construct. **This phase (re-)introduces NO new edition predicate.**
> 
> ---


### PHASE-05 record (absorbed from the retired phase doc)

> (no STATUS block found)


### PHASE-06 record (absorbed from the retired phase doc)

> `DONE (2026-07-11, DEVLOG 767-775) — all 6 exit criteria verified; final battery 3159 conformance (incl. all
> OoSpineTests shadowing goldens + the new interface-crossing test) / 281 unit / 32 characterization BYTE-IDENTICAL
> / FULL legacy guard NIST 353 MATCH / CI GREEN in BOTH configurations. Step 8 extras: the manual CLI smokes ran
> with SPEC-CORRECT output (OO method-local shadowing per §8.4.6.2.1 r3a; ODO + whole-group image byte-exact); a
> 9-agent ADVERSARIAL PHASE REVIEW (5 finders x 2 refuters) confirmed ONE pre-existing latent defect — the
> crossing-form harmonize missed interface-IMPLEMENTATION pairs (override chains only) — FIXED in the close commit
> with a proven-to-bite conformance test (DEVLOG 775); and the CI-red retrospective: the Step-6 watermark gate
> first landed [Conditional(DEBUG)], CI's RELEASE leg stripped it (3 red pushes) — now ALWAYS-ON (DEVLOG 774).
> Per-step ledger (each landed battery-green + pushed):
> 
> - Step 0 (preflight): baseline recorded (3158/269/32 + guard 353 MATCH). AS-BUILT DEVIATIONS from this doc's
>   snapshot, recorded per the design-currency rule: (a) the pass scaffolding lives in Binding/Passes/ (not
>   Binding/Pipeline/) as IBindPass/BindPass delegate records over a PassPhase watermark enum (not a Capability
>   enum); (b) the P3 Step-14a bind/emit split ALREADY existed (CallBindRunUnit -> BoundRunUnit -> CallEmitRunUnit)
>   and the driver already gated between bind and emit; (c) the FILE whole-group loop was already the
>   MarkFileRecordImageLeaves RESOLVE pass (statement binding consults IsCharacterImage - ST102A - so it CANNOT
>   move to the group tail; supersedes this doc's Step-3 "fold the FILE loop into StorageFormPass"); (d)
>   UsageCollectionPass + StorageFormPass.Compute (D0 prove phase) already ran in the bind half.
> - Step 1: DELIVERED BY P5 Step 3 (manifest + ValidateDag + BindPipelineTests already existed) - no churn added.
> - Step 2 (commit 8ac37480, DEVLOG 767): BinderDriver extracted -> immutable BoundCompilation; bound model types
>   relocated to Binding/Model (CallUnit->BoundUnit + CallBridge, OoClassUnit, BoundRunUnit->BoundCompilation -
>   OoClassUnit moved DESPITE this doc's "keep it" so Binding never references a CodeGen type);
>   IOoBindHost + BindSession = the P6->P9 seam (an INTERFACE, not the doc's OoBindCallbacks delegate record);
>   file-connector qualification + AnyFiles moved into Bind; emitter = Bind shim + EmitBound(comp).
> - Step 3 (commit 514d9c73, DEVLOG 768): the middle-end tail is the DECLARED BindPipeline.GroupTail manifest
>   (ProcedureBinding -> UsageCollectionPass -> StorageFormPass over GroupBindContext);
>   ValidateFullChainOnce validates resolve-prefix ++ group-tail as ONE chain; StorageFormPass.Run owns
>   temp-resync -> MarkStoreAsImage -> OO-harmonize -> Compute.
> - Step 4 (commit 578ad5d1, DEVLOG 769): VersionConformancePass is the NAMED terminal GroupTail pass (new
>   PassPhase.EditionConformanceChecked; Run takes GroupBindContext, which carries the parse Tree); the driver no
>   longer references the pass - Phase 2 = Bind -> gate-on-sink -> CheckOnly -> EmitBound. Exit criteria #4/#6
>   verified (the sole non-pass ConstructRegistry.Check is the documented UDF-recognition exception,
>   StatementBinder.Udf.cs); CheckOnlyCompileTests strengthened (band-code + no-emit-artifact asserts); INV-1
>   continuity sweep all-OK.
> - Step 5 (commit 52d32ae6, DEVLOG 770): a 3-agent audit proved ZERO CodeGen/Validation writes into the binder
>   model; the two misplaced bind-time bodies moved to Binding (harmonize ->
>   StorageFormPass.HarmonizeOverrideCrossings with OoClassTable.StringCarried as the ONE crossing-form predicate;
>   OoQualifyClassFiles -> BinderDriver.QualifyClassFiles); the IOoBindHost seam shrank to the 4 OO-bind members;
>   the 14 CodeGen-read collections are IReadOnly views over private backings (cross-class write channel =
>   DataBinder.SeedInheritedGlobalIndex). Element mutability + the ByName/Conditions maps: data-model-track /
>   Step-7-builder scope, documented in the property docs.
> - Step 6 (commit c5521a63, DEVLOG 771; CORRECTED per DEVLOG 774): DataBinder.Watermark/MarkProduced/Require
>   (THROWING, ALWAYS-ON — the first landing was [Conditional(DEBUG)] and CI's RELEASE test leg stripped the call
>   sites, failing the throw-expecting tests three pushes running; the fix keeps the guard in every configuration:
>   a mis-ordered pass in a production compiler is a silent miscompile, and the cost is integer compares);
>   BindResolve + the BinderDriver group-tail loop Require-then-Mark per pass per binder; the CapacityRegisters
>   getter carries the flagged late-fact guard; Tier/ClassOffset/Storage item-level guards deferred to P7 (no
>   DataItem->binder backref) with pass-entry Requires covering the order; WatermarkTests x4; the full conformance
>   run is the never-fires proof.
> - Step 7 (commits a69b6fd9 + 7b, DEVLOG 772/773): Binding/Model/SymbolTable.cs is THE one scope-aware resolver
>   (TryResolve / TryResolveCondition / TryResolveIndex / IndexCellOf over an explicit Scope);
>   DataBinder.Symbols per binder + ActiveScope/ScopeOf. 7a shimmed the quadruple byte-equivalently (full
>   battery); 7b migrated all ~16 sites and DELETED LookupData/LookupDataInScopeOf/TryGetVisibleIndexField/
>   IndexFieldFor (grep clean; OO shadowing goldens + byte-identical snapshots green). DEVIATIONS: ONE table PER
>   BINDER, not BoundCompilation.Symbols (COBOL name scopes are per-unit; the doc's single-table sketch presumed
>   a merged namespace that does not exist); IndexCellOf stays a SEPARATE shape beside TryResolveIndex
>   (IndexFieldFor's callers pass a table's DECLARED index-name, where the data-name-shadow check must NOT
>   apply). SymbolTableBuilder-owned storage deferred to P7 per this doc's own 7b.3 option;
>   ReferenceResolver.ResolveUnqualified + StatementBinder:1766 carry the same precedence inline - P7 candidates
>   (DEVLOG 773).`
> 
> > The executing session MUST update this line as it goes: `IN PROGRESS @ step N` after each completed step, and
> > `DONE` once Verification (§5) passes. If resuming, read this line first, then re-run Step 0's battery to confirm
> > the tree is green before continuing.
> 
> ---


### PHASE-07 record (absorbed from the retired phase doc)

> (no STATUS block found)


### PHASE-08 record (absorbed from the retired phase doc)

> `DONE` (2026-07-15 — DEVLOG 841–843; three verdict-gated commits, battery green at each: conformance 3256 ·
> unit 281 · legacy guard NIST 353 MATCH / ALL GREEN)
> 
> **As-landed record (deviations from the recipe, all within its license):**
> - Executed in BATCHED cycles (the standing P7 discipline): commit 1 = Steps 1–4, commit 2 = Steps 5–9,
>   commit 3 = Step 10 + the gate-driven `ExternalSwitches` conversion. Step 6's audit result is recorded in
>   DEVLOG 842 (notably: `ExternalStore.Cell` IS emitted ⇒ shim kept).
> - Naming as landed: instance `ProgramTable` / `ExceptionEngine` (the doc's own clash resolution) /
>   `ExternalTable` (shim `ExternalStore`) / `ModuleStack` / `SwitchStore` (shim `ExternalSwitches`) /
>   `FileRegistry` + `IO/Sharing/PhysicalFileTable`.
> - BEYOND-RECIPE (the §5 hidden-mutable-static gate found it): `ExternalSwitches`' switch cache was a
>   process-global static holding run-unit-scoped state (§12.3.7 GR4 NOTE 1) — converted to `SwitchStore` on
>   `RunUnit` + static shim. Surviving statics are all genuinely immutable (`ExceptionCatalog`, `Pow10` tables,
>   `SystemClock.Instance`).
> - Exit criteria: 1 ✅ (`RunUnit.Run` + `ResetCurrent` reproduce the old reset semantics; the DEFAULT lazy-ambient
>   path keeps the emitted driver unchanged) · 2 ✅ (`Keyed*` + the fallthrough DELETED; ONE polymorphic
>   `FileRegistry`) · 3 ✅ (`Pow10` single-sourced, six copies deleted) · 4 ✅ (RL/IX/SQ/IC NIST + numeric unit +
>   full conformance green) · 5 ✅ (byte-stable BY CONSTRUCTION — zero compiler-side files changed in the phase;
>   `git diff 534f2253..HEAD -- src/Cobol.Net.Compiler` is empty).
> 
> ---


### PHASE-09 record (absorbed from the retired phase doc)

> (no STATUS block found)


### PHASE-10 record (absorbed from the retired phase doc)

> **STATUS: DONE (2026-07-17).** All 17 steps closed across 14 battery-gated commits (7436a1ef → a0fd3f68 + the close-out; DEVLOG 854–870): the Step-1 reconciliation audit, then the waves — national · EC-N · concat · allocate · UDF category-RETURNING · CONSTANT · pointers/PROGRAM-POINTER · file-lock · UDF BY-VALUE/per-evaluation · recursive-WS/LOCAL-STORAGE · ARITH-STANDARD · TYPEDEF/SAME-AS/EXTERNAL-type · RW-2002 (PRESENT WHEN/VARYING/multi-COLUMN) · ALPHABET-national. **Exit criteria confirmed:** every track has a greenfield-discovered positive corpus + matrix rows + negatives; the M2 catalog marks flipped to greenfield truth (this doc's verdict table is the per-track record; `ISO2023_CONFORMANCE_PLAN.md` + `PHASE4_RECONCILIATION.md` carry the per-wave flips); national CharImage one-UTF-16-char-per-position pinned (`NationalStorageFormTests` + goldens); final battery **3467 conformance · 292 unit · 33 characterization byte-identical · legacy 1196+636 · NIST 353 MATCH**, CI green on every commit. **Ledgered residues carry forward BY NAME** (the §"Genuine open residue" ledger + each step's as-built): the per-shape 1510 UDF RETURNING residues, OPTIONAL formals, the two recursive-WS stages, OO class-unit BASED, INITIALIZE-over-pointer-categories, line-seq 06/09/71 + REWRITE + the LINE SEQUENTIAL gate, keyed GR10a FPI + keyed ADVANCING emission, cross-run-unit sharing, SORT national-key carry (RESIDUE-11), multiple-LINE repetition (with the report-OCCURS family), the narrowed 1509 shapes, the signed-leaf strong ordering, B-SHIFT/BX (2023 → P13), STANDARD-BINARY (2014 pending row), MAX/MIN-under-explicit-collating (both classes).
> _(The executing session updates this line: `NOT STARTED` → `IN PROGRESS @ step N (<short note>)` → `DONE`. Keep the per-step checkboxes in §4 current in the same commit that lands each step.)_


### PHASE-11 record (absorbed from the retired phase doc)

> ```
> STATUS: DONE (2026-07-17, DEVLOG 871–879; 9 battery-gated commits 2a0ab666→19dfe579+close, CI green on each)
> ```
> 
> > **ALL FIVE EXIT CRITERIA MET.** (1) **Zero `Deferred` intrinsic rows** — every §15 row binds
> > `Runtime`/`Fold`/`Unsupported` (grep-verified). (2) **Tier-C decided** — the rejection is single-sourced (the
> > REDEFINES-class reject was already the ONE `RedefinesClass.Classify` mutator; Step C collapsed the ~12
> > scattered classless-mixed-usage-group emit guards onto the ONE `TierCIsland.Reason`, predicates preserved);
> > the confined-`byte[]` codec (Step D) is DEFERRED as a scheduled increment (DESIGN-data-model §2.3). (3)
> > **Every promotion has a value-exercising golden** (`intrinsics_boolean_conv`, `intrinsics_date_window`,
> > `intrinsics_test_validators`, `intrinsics_byte_length`, `intrinsics_smallest_algebraic` + the spec-pinned
> > `IntrinsicFunctionDifferentialTests` facts + `CobolDateWindowingTests`/`TestNumvalScannerTests`). (4)
> > **Every promotion has a window/disposition negative row** (`*_below_2002` / `smallest_algebraic_below_2023`
> > → COBOLNET1502; `locale_functions_a49` / `locale_keyword_a49` → COBOLNET1518; matrix rows
> > `boolean-of-integer-2002` / `date-to-yyyymmdd-2002` / `test-numval-2002` / `byte-length-2002` /
> > `smallest-algebraic-2023`). (5) **Full battery green:** conformance **3521** · unit **301** · characterization
> > **33** · legacy 1196+636 · NIST **353 MATCH**.
> >
> > **⚠ SCOPE CHANGE (spec-faithfulness):** CONCATENATE is NOT an ISO function at any edition (the re-scout found
> > zero spec occurrences; §15.18 CONCAT is new-in-2023) — the "implement with window [2002,2023)" plan was audit
> > drift; the greenfield catalog row is DELETED (a reference draws COBOLNET1501). CONCATENATE-as-a-vendor-extension
> > is a separate future call.
> >
> > **DEFERRED to a scheduled increment (Step D):** the confined `byte[]` REDEFINES codec — the one sanctioned
> > `byte[]` boundary of invariant #1. Its §2.5/§3 design is stale vs. the as-built code (no `RedefinesClassifier`
> > type; `RedefinesTier.ByteCanonical` dead-by-construction; `StorageForm.TierCWindow` has no Read/Write) and no
> > NIST program requires it — it needs a fresh design pass (recorded in DESIGN-data-model §2.3).
> >
> > **Retained reference:** `PHASE-11-scout-notes.md` (the persisted anchor re-scout — verified §/GR/SR anchors +
> > spec line numbers, hand-derived golden values, the Tier-C guard-site inventory) is kept as a durable
> > spec-to-code reference (its forward-resume framing is now historical).
> > The executing session updates this line to `IN PROGRESS @ step N` and finally `DONE`. Keep it in sync with the per-step checkboxes in §4.
> 
> > **Step 0 baseline (2026-07-17, tree at `45fe74dd`, all green):** greenfield conformance **3467/3467** ·
> > greenfield unit **292/292** · the legacy guard `guard-fast.sh` **NIST 353 MATCH, 0 regressions** (legacy
> > unit 1196 + integration 636 ALL GREEN) · solution build 0 warnings 0 errors.
> >
> > **The Step-1 enumeration is DONE:** the live `IntrinsicBind.Deferred` set is **17 rows** (catalog lines
> > 133–174) = §3.1's 22 minus the 5 P10-landed rows (CHAR-NATIONAL, DISPLAY-OF, NATIONAL-OF,
> > EXCEPTION-FILE-N, EXCEPTION-LOCATION-N — already `Runtime`, verified in the catalog source).
> >
> > **The P10-lesson anchor re-scout is DONE** (11 parallel scouts over the spec + code, 2026-07-17):
> > **`PHASE-11-scout-notes.md`** carries the full verified findings — exact §/GR/SR anchors with spec line
> > numbers, hand-derived golden values per family, the end-to-end code seams (catalog row → binder →
> > renderer → runtime, with file:line), the conformance/matrix/negative test wiring (the P10
> > `exception_file_n` worked example), and the complete Tier-C guard-site inventory. **Its ⚠ gotcha /
> > discrepancy blocks OVERRIDE this doc where they conflict** (e.g. `ComputeTier` is at
> > `DataBinder.cs:2376`, not ~1752; BOOLEAN-OF-INTEGER(0, n) is an ambiguity resolved accept-0; left
> > truncation to argument-2 bits is NORMAL per §15.13.4 r1 + Annex D.10). Do NOT re-scout — read the notes.
> 
> ---


### PHASE-12 record (absorbed from the retired phase doc)

> (no STATUS block found)


### PLAN-bindtime-gating-migration record: COMPLETE — the introduction-gating migration to the bind-time `ConstructRegistry.Check` landed across P03–P06 (Exec Step E); the two-arm `VersionConformancePass` is the SOLE edition gate.
