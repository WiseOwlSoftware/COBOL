# PHASE 13 — M4 (COBOL-2023) deltas + EC remnants + Table 1/5 behavior-row burn-down

- **Phase:** P13
- **Track:** feature-iso
- **Risk:** HIGH
- **Depends on:** P11 (deferred-intrinsics backlog to zero + Tier-C REDEFINES confined-byte codec), P12 (M3 / COBOL-2014 deltas: dynamic length, TYPEDEF edges, >>PROPAGATE, IEEE floats, function pointers).
- **Blocks:** P14 (matrix closure + in-repo greenfield guard + one-time equivalence proof).

> **GOAL (one paragraph).** Land the remaining COBOL-2023 language surface, close the ISO exception-condition (EC)
> remnants that no earlier feature wave installed, and disposition (implement, gate, or document-as-non-support)
> every orphaned edition-change and behavior row so the "four-compilers-in-one" version matrix has **no undecided
> cells left for the 2014→2023 delta and no un-dispositioned Table 1/5 behavior row**. Concretely: wire
> EC-SIZE-TRUNCATION into every arithmetic store (which finally unblocks ROUNDED MODE IS PROHIBITED end-to-end) and
> add EC-BOUND-OVERFLOW / EC-BOUND-REF-MOD; add the 2023 new-feature constructs (boolean shift operators, group
> SYNCHRONIZED, NO SIGN, dynamic-length SET, CONTINUE timed pause, INSPECT BACKWARD residue, PICTURE EDITING phrase,
> EXCEPTION-FILE connector argument, PERFORM WHEN exception-checking / UNTIL EXIT, the DELETE FILE statuses, the
> COBOL-WORDS/PUSH/POP/DISPLAY/FLAG-14 directive quintet, the EXTERNAL run-unit conformance cluster, GOBACK status
> phrase) each with its below-2023 rejecting diagnostic; flag the §4.2.12/§4.2.13 archaic/obsolete rows; run the
> one-pass A.3 processor-dependent disposition sweep with §4.2.6 warnings; burn down the ~44 Table 1/5 behavior
> rows (I-O status semantics, VALUE clause conformance); and emit documented non-support diagnostics for the four
> facilities the project does NOT implement (MCS asynchronous messaging, commit/rollback, VALIDATE, screen).
>
> **Gating mechanism (canonical — `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`):** a NEW
> edition-gated construct = a `constructs.json` row + a superset grammar rule (stamped with its construct-id
> annotation, or a self-identifying bound node) + a `VersionConformancePass` rule + a negative fixture at EVERY
> earlier edition — NEVER a new parse-time edition predicate (unless a proven load-bearing ambiguity needing a
> forward-detect) and NEVER a binder-embedded `Check`.

## EXIT CRITERIA (the bar for "DONE")
1. **VCR Tables 2/3 rows dispositioned** — every 2014→2023 row in `docs/VERSION_CHANGE_REFERENCE.md` Tables 1/2/3
   is either `GATED`/`done` (implemented with its below-2023 diagnostic + a conformance golden) or carries an
   explicit, cited `documented-non-support` disposition (never a bare `TODO`).
2. **Table 1/5 behavior rows implemented or per-row dispositioned** — the ~44 behavior rows (Table 1 "Affects
   existing? = Yes" and the Table 5 FLAG-02/FLAG-14 rows) are each implemented-and-gated or dispositioned with a
   one-line reason + citation.
3. **EC surface closed** — every EC condition this phase raises (EC-SIZE-TRUNCATION, EC-BOUND-OVERFLOW,
   EC-BOUND-REF-MOD, EC-CONTINUE-*, EC-EXTERNAL-*) has a conformance golden that raises it under `>>TURN … CHECKING
   ON` and observes it via `EXCEPTION-STATUS`/`EXCEPTION-FILE`/a USE declarative.
4. **Full battery green** — greenfield conformance + unit + the FULL legacy guard (NIST 353 MATCH) all green at
   every commit boundary; per `docs/rearchitecture/DESIGN-test-build-ci.md` gates 1 & 2 red = stop.

## STATUS
`IN PROGRESS — Step 1 (audit) + Wave B partial + Wave C batch 1 (7 constructs) DONE; RESUME AT Wave C batch 2` (branch `phase-13-m4-2023`; NOT merged)

> **Wave C batch 1 (7 of 10 constructs) landed** — spec-first from the persisted re-scout
> `docs/rearchitecture/PHASE-13-wave-c-scout.md` (which caught the SET-SIZE-OF-not-LENGTH-OF + EC-STORAGE-not-EC-BOUND
> + boolean-shift-precedence audit drifts): GOBACK status (presence-only), USAGE PACKED-DECIMAL WITH NO SIGN,
> 63-char words, SET [SIZE OF] dyn-length, CONTINUE AFTER SECONDS + EC-CONTINUE-LESS-THAN-ZERO, PERFORM UNTIL EXIT,
> boolean shift B-SHIFT-L/R/LC/RC (Table A.2 oracle byte-exact). Diag band 1565–1568 consumed. See DEVLOG 887.
> **Wave C batch 2 (remaining):** WRITE BEFORE AND AFTER + SUPPRESS WHEN alt-key (C5), PICTURE EDITING phrase (C3),
> PERFORM Format 3 exception-checking (staged large), + the STOP/GOBACK exit-code VALUE wiring slice.

> **RESUME POINTER (a new session): read `resume-prompt.md`'s top P13 banner FIRST — it has the ordered resume
> steps + the batched remaining-work waves.** LANDED: Step 1 = the as-built audit (`PHASE-13-audit.md` — THE
> worklist: 18 DONE / 21 PARTIAL / 31 MISSING; evidence in `PHASE-13-scout-notes.json`); Wave B = EC-SIZE-TRUNCATION
> verified+goldened (§14.7.5). STAGED: EC-BOUND-OVERFLOW/REF-MOD (catalogued, not raised — needs the ambient-gate
> pipeline). NEXT: Wave C = the 2023 grammar constructs (batch by the shared full-legacy-guard gate). ⚠ the plan's
> "band head 1538 / from 1539" is STALE — P12 took 1561-1564; use **1565+** (grep to confirm).

> **The as-built audit is DONE — see `PHASE-13-audit.md` (the worklist) + `PHASE-13-scout-notes.json`.** 71 items:
> 18 DONE (verify-and-flip only), 21 PARTIAL, 31 MISSING. The genuinely-remaining work is batched by GATE type
> (owner optimization directive): grammar constructs share ONE full legacy guard per batch; binder/runtime + doc
> rows use the greenfield battery only. Each feature is still individually CLI-probed + ships its golden + a
> below-2023 negative in its batch commit.

> **Executing session: update this line** to `IN PROGRESS @ step N` as you work, and to `DONE` when the exit
> criteria are met. Append a one-line note per commit boundary you cross (commit SHA + step numbers).

---

## 1. Rationale — the problems this phase fixes

The compiler is spec-first (behavior defined by `specs/ISO_COBOL.md`, cited by §; the legacy oracle and NIST
goldens are regression nets with known holes — memory `feedback_use_the_spec`). Prior phases greened COBOL-85 and
landed most of the 2002/2014 feature catalog, but three classes of work were left un-dispositioned:

1. **Orphaned 2014→2023 edition-change rows.** `docs/VERSION_CHANGE_REFERENCE.md` (VCR) is the 130+-row
   edition-change checklist. As of the pre-phase snapshot, Tables 1/2/3 still carry ~30 `TODO` rows (rows 3, 8–37
   except those marked GATED; 38–43; 45–88) that no feature wave touched — e.g. row 3 (CALL … ON OVERFLOW removal),
   row 9/46 (boolean shift operators), row 42 (NO SIGN), row 43 (group SYNCHRONIZED), row 57 (CONTINUE timed pause),
   row 62 (PICTURE EDITING phrase), rows 68/69 (EXCEPTION-FILE[-N] connector argument), rows 79/80 (PERFORM
   exception-checking / UNTIL EXIT), row 75 (GOBACK status), the directive rows 55/59/64/81/11, the EXTERNAL cluster
   rows 15/16/18/31/63. Each is a co-equal G1 obligation: implement the 2023 behavior AND emit a specific below-2023
   diagnostic naming the construct + the introducing edition (VCR "How to use" step 3).

2. **EC remnants no wave installed.** The EC engine (`>>TURN`, RAISE/RESUME, USE F3, RAISING propagation, the
   status→EC bridge, EXCEPTION-* functions) is DONE and the catalog is complete
   (`src/Cobol.Net.Runtime/Exceptions/ExceptionCatalog.cs` — all 2002/2014/2023 names, IntroducedIn-tagged), but
   several conditions are catalogued yet never RAISED by the code that should raise them:
   - **EC-SIZE-TRUNCATION** is wired only for the *receiver-capacity* store in `CodeGen/Verbs/ArithmeticEmitter.cs` behind
     `ecState.SizeErrEcVar`; the scope note says it must be "wired into arithmetic stores" so ROUNDED MODE IS PROHIBITED
     raises it (today PROHIBITED fires ON SIZE ERROR at the observable level only — ISO2023 plan §M2-ARITH-1
     follow-up #2). VCR row 53 (§14.7.5) clarifies EC-SIZE-TRUNCATION is raised by rounding *only* under
     DEFAULT/statement ROUNDED MODE IS PROHIBITED.
   - **EC-BOUND-OVERFLOW** (§8.5.1.9) and **EC-BOUND-REF-MOD** (VCR row 30, §14.9.x, 2023 REF-MOD-ZERO-LENGTH) are
     flagged as follow-ons (memory `project_greenfield_state`: OCCURS DYNAMIC left EC-BOUND-OVERFLOW "LOUD today,
     never silently wrong" but not raised as the named nonfatal condition).
   - **USE FOR DEBUGGING + DEBUG-ITEM** stage LOUD as COBOLNET0899 today (VCR row 7.17: "the full register/trigger
     facility is deferred with the golden-less DB series"). The EC-remnant scope wants the DEBUG-ITEM register model
     and USE FOR DEBUGGING implemented at `--std 85` (where it is legal) with the below-2002 removal gate already in
     place.

3. **Un-dispositioned behavior rows + facilities.** The ~44 Table 1/5 behavior rows (I-O status '04'/'07'/'0x'/'37',
   VALUE-clause numeric-edited conformance rows 34/35/36/86, MERGE-in-output-procedure row 27, transfer-of-control
   sections row 33, WRITE END-OF-PAGE row 37) are edition-dependent semantic changes no feature wave owns. And four
   whole facilities — MCS asynchronous messaging (VCR 38), commit/rollback (VCR 39), VALIDATE (VCR 95/117–125/129),
   screen — are NON-goals for this project but must not silently mis-parse: §4.2.6/§4.2.7 require a **compile-time
   warning** naming the unsupported element. Today they variously parse-and-drop or hit a generic COBOLNET0899.

**Design note — much of this scope is ALREADY BUILT; audit first.** Several named scope items landed in earlier
waves and only need *verification + a matrix/VCR status flip*, NOT re-implementation:
- **DELETE FILE** — DONE: `BoundKeyedDeleteFile` (`Binding/Procedure/Verbs/KeyedIoBinder.cs:155`, §14.9.10 Format 2), goldens
  `tests/conformance/2023/delete_file{,_absent}.{cob,out}`. Its **5 new I-O statuses** ('05','37','39','41','62',
  VCR row 78) are the residual runtime work.
- **INSPECT BACKWARD** — DONE: `Binding/Procedure/Verbs/InspectBinder.cs:41` (COBOLNET0845 below 2023), golden
  `tests/conformance/2023/inspect_backward`. (Confirm the CONVERTING-with-BACKWARD residue.)
- **Logical XOR / EXCLUSIVE-OR** (VCR 41) — GATED, golden `logical_xor`.
- **The 2023 intrinsics** CONCAT/BASECONVERT/CONVERT/FIND-STRING/MODULE-NAME/SMALLEST-ALGEBRAIC/SUBSTITUTE/TRIM
  (VCR 65–74) — DONE, goldens present.
- **GOBACK RETURNING** — DONE at 2002. The 2023 **status phrase** (`GOBACK … WITH … STOP`, VCR 75) is
  the residual delta — verify whether P9 covered it; if not it is in this phase's scope.
- **EXIT METHOD/FUNCTION windows, EXIT PROGRAM/NEXT SENTENCE archaic, MOVE-alphanumeric-figurative, method-WS
  window** (VCR 5/6/74/89/90/1/130e) — GATED already.

The FIRST step of this phase is therefore an **as-built audit** that produces a per-row disposition table; only the
genuinely-missing items get implemented. This avoids re-doing landed work and keeps the phase's true size honest.

**Out of scope (explicit):** JSON/XML (non-ISO, 0 spec occurrences — the dead grammars are removed in P1); the
in-repo greenfield guard + the one-time equivalence proof (P14); the god-class/rearchitecture refactors (P2–P8).

---

## 2. Target end-state for this phase (concrete)

When this phase is DONE the following exist / hold:

### New / changed source
- **`src/Cobol.Net.Runtime/Exceptions/ExceptionState.cs`** — gains `SizeTruncationChecking`, `BoundOverflowChecking`,
  `BoundRefModChecking`, `ContinueChecking`, `ExternalChecking` nonfatal-gate flags mirroring the existing
  `ArgumentFunctionChecking`/`DataConversionChecking` pattern, plus a `RaiseSizeTruncation()`,
  `RaiseBoundOverflow()`, `RaiseBoundRefMod()`, `RaiseContinueLessThanZero()` set of `Set(...)`-delegating helpers.
- **`src/Cobol.Net.Compiler/Binding/Procedure/Verbs/EcBinder.cs`** — the arithmetic-store EC wrap
  (`SizeNames`) is honored at the store site for **every** arithmetic verb (ADD/SUBTRACT/MULTIPLY/DIVIDE/COMPUTE),
  not just receiver-capacity, so ROUNDED MODE IS PROHIBITED latches EC-SIZE-TRUNCATION.
- **`src/Cobol.Net.Compiler/CodeGen/Verbs/ArithmeticEmitter.cs`** — the `SizeErrEcVar` latch is emitted
  at the PROHIBITED-inexact and receiver-capacity paths under `EC-SIZE` checking.
- **Grammar** (`src/Cobol.Net.Frontend/Grammar/Core/*.g4`) — new SUPERSET alternatives (each stamped with its
  committed-match construct-id annotation, or reaching a self-identifying bound node — never a parse-time edition
  predicate) for: boolean shift
  operators (`B-SHIFT-L/R/LC/RC`), group `SYNCHRONIZED`, `NO SIGN` on USAGE, `SET id TO LENGTH …` /
  `SET LENGTH OF id …` dynamic-length, `CONTINUE AFTER expr SECONDS`, the `EDITING` PICTURE phrase, `EXCEPTION-FILE`/
  `EXCEPTION-FILE-N` optional file-connector argument, `PERFORM … WHEN` exception-checking + `PERFORM … UNTIL EXIT`,
  `GOBACK … WITH … STOP` status phrase, `WRITE … BEFORE AND AFTER ADVANCING`, `SUPPRESS WHEN` on ALTERNATE RECORD KEY,
  and the `>>COBOL-WORDS`/`>>PUSH`/`>>POP`/`>>DISPLAY`/`>>FLAG-14` directives in the preprocessor.
- **`tests/version-matrix/constructs.json`** — one registry row per new 2023 construct (introduced-in 2023) and one
  per behavior-change row that enters the matrix, with pinned diagnostic codes.
- **`docs/VERSION_CHANGE_REFERENCE.md`** — every Table 1/2/3/5 row this phase touches flipped from `TODO` to
  `GATED`/`done (pin-to-spec)`/`documented-non-support` with the DEVLOG # + diagnostic code.
- **`tests/conformance/2023/`** — a new `<name>.cob` + `<name>.out` golden per implemented construct (auto-discovered
  by `ConformanceTests`; listed in `tests/conformance/2023/manifest.json`).
- **`tests/conformance/85/` (or the negative matrix)** — a below-2023 rejecting-diagnostic case per new construct.
- **`docs/CONFORMANCE.md`** (new, or a § added to an existing doc) — the §4.2.6 processor-dependent / §4.2.7 optional /
  §4.2.12 archaic / §4.2.13 obsolete disposition list required by §4.2.16 (User documentation). This is the
  "one-pass A.3 disposition sweep (46 items)" written down.
- **DEBUG facility** — `USE FOR DEBUGGING` + the `DEBUG-ITEM` special register implemented at `--std 85` (the removal
  gate to ≥2002 is already `use-for-debugging-removed-2002` / VCR 7.17); DB-series NIST programs move from
  golden-less residue toward compiled-and-run where feasible.

### Diagnostics
- New below-2023 introduction diagnostics use **`COBOLNET0900`** (Introduction), raised by the construct's
  **`VersionConformancePass`** rule (fed by the construct-id annotation the superset grammar rule stamps, or by a
  self-identifying bound node), so they enter the version matrix automatically (per
  `docs/rearchitecture/DESIGN-version-conformance-pipeline.md` — route new gates through the pass, never a
  parse-time edition predicate, a binder-embedded `Check`, or an inline `if`).
- Bind-dependent semantic rejections / staged-loud residue continue the **15xx feature band**. The band head is
  **1538** at phase start (`COBOLNET1538`); allocate **contiguously from 1539** as steps land (see the allocation
  table in §4). Record each in the diagnostic-code map (DEVLOG banner + `docs/COBOLNET_DESIGN.md`).
- Obsolete/archaic flags use **`COBOLNET0903`** (ObsoleteFlag, always a warning); "the four archaic VCR rows get
  their own 0903 sub-code" → append a discriminating suffix in the message text and a distinct `constructs.json`
  row id per element (0903 is the shared code; the row id is the sub-identity).
- Documented-non-support facilities (MCS/commit-rollback/VALIDATE/screen) emit a **new `COBOLNET1560`-band
  "recognized, not supported (processor-dependent/optional, §4.2.6/§4.2.7)"** warning-or-error per the facility's
  conformance category (VALIDATE is obsolete-optional → warning; MCS/commit-rollback are processor-dependent →
  §4.2.6 warning + no executable code required).

### Invariants preserved (non-negotiable)
- Typed-native data only (no byte substrate; the only `byte[]` boundary is Tier-C REDEFINES / file records).
- The full battery is green at every commit boundary (gates 1 & 2 hard).
- Singular pattern: new EC gate flags follow the ONE `ExceptionState.XxxChecking` pattern; new edition gates route
  through the ONE `VersionConformancePass` funnel (the grammar stays a superset; the binder stays edition-agnostic);
  no parallel mechanism.

---

## 3. STEP-BY-STEP

> **Conventions.** Build the whole vertical for each feature (grammar → binder/bound tree [all semantics here] →
> emitter → runtime → output-verifying conformance test) — memory `feedback_parse_and_emit_together`. One feature at
> a time; compile + run after every change (`feedback_test_after_every_change`, `feedback_iterate_one_at_a_time`).
> A **shared-`.g4` change requires the FULL legacy guard** in the same change set (memory
> `feedback_autonomous_grammar_nist`); a binder-only change needs only the greenfield battery. Every post-1985
> feature ships its conformance test in the SAME commit (`feedback_conformance_tests_per_feature`). Grammar changes
> are pre-authorized (owner grant 2026-07-05). Every commit gets a DEVLOG entry (newest-first).
>
> **Prebuilt CLI for ad-hoc reproduction:**
> `dotnet E:/CobolSharp/src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll <src.cob> --std 2023 -o <out.dll> --run`
> (use `--std 85`/`2002`/`2014` to prove the below-edition rejection).
>
> **Full greenfield battery** (run at every commit boundary):
> `dotnet test E:/CobolSharp/tests/Cobol.Net.Tests.Conformance` and
> `dotnet test E:/CobolSharp/tests/Cobol.Net.Tests.Unit` (exact project names may differ — discover with
> `find tests -name '*.csproj'`).
> **Full legacy guard** (only when a `.g4`/lexer changed): `bash scripts/guard.sh` (or `scripts/guard-fast.sh` for
> iteration) — must report NIST 353 MATCH, 0 regressions.

### STEP 0 — Preconditions & battery baseline
- **Do:** Confirm P11 and P12 are `DONE` (read their phase docs' STATUS lines). Confirm the tree builds and the
  battery is green *before* touching anything.
- **Command:** `dotnet build E:/CobolSharp/src/Cobol.Net.Cli` then the full greenfield battery + `bash scripts/guard.sh`.
- **Expected:** clean build; conformance + unit green; NIST 353 MATCH.
- **Why:** a HIGH-risk feature phase must start from a known-green baseline so any red is attributable to this work.
- **Not a commit boundary.**

### STEP 1 — AS-BUILT AUDIT (produce the disposition table; NO code change)
- **Do:** For each scope item, determine its current state by grepping the tree and running the CLI. Produce a table
  `docs/rearchitecture/PHASE-13-audit.md` with columns: *Item | VCR row(s) | Spec § | Current state (DONE / PARTIAL /
  MISSING / N/A) | Evidence (file:line or golden) | Action this phase*. Seed it from these known-DONE items (verify
  each, do not trust): DELETE FILE (+audit the 5 statuses), INSPECT BACKWARD, XOR, the 8 2023 intrinsics, GOBACK
  RETURNING, the EXIT/archaic/method-WS windows. Then classify every remaining VCR Table 1/2/3/5 row and every EC in
  the §1 remnant list.
- **Commands (representative):**
  - `grep -rn "SYNCHRONIZED\|B-SHIFT\|NO SIGN\|UNTIL EXIT\|CONTINUE AFTER\|EDITING\|SUPPRESS WHEN\|COBOL-WORDS" src/Cobol.Net.Frontend/Grammar`
  - `grep -rn "EC-BOUND-OVERFLOW\|EC-BOUND-REF-MOD\|EC-CONTINUE\|EC-EXTERNAL\|SizeTruncationChecking" src/Cobol.Net.Compiler src/Cobol.Net.Runtime`
  - `grep -rn "DEBUG-ITEM\|USE FOR DEBUGGING" src/Cobol.Net.Compiler`
  - Run one probe `.cob` per uncertain feature at `--std 2023` and inspect for COBOLNET0899 (false-green / staged) vs
    a real bound node.
- **Expected output:** a complete table with NO "unknown" cells; every VCR row mapped to DONE / this-phase-step.
- **Why:** the scope overlaps heavily with landed work; skipping this re-implements finished features and hides the
  true remaining set. The table is the phase's worklist and the resumability anchor.
- **COMMIT BOUNDARY.** `docs(cobolnet): Phase 13 kickoff — as-built audit + disposition table for the 2023/EC/behavior-row scope`

---

### WAVE B — EC arithmetic-store remnants (unblocks ROUNDED MODE PROHIBITED)

### STEP 2 — EC-SIZE-TRUNCATION wired into arithmetic stores
- **Spec:** §14.7.5 (size-error / ROUNDED MODE IS PROHIBITED — VCR row 53 clarification), §14.6.13.1.6 Table 13
  (EC-SIZE-TRUNCATION = Fatal), §8.8.1 (arithmetic). Editions: the EC model is 2002+; the PROHIBITED→truncation
  clarification is a 2023 pin (VCR 53 "Affects existing? = No" → pin-to-spec for all EC-enabled editions).
- **Files:**
  - `src/Cobol.Net.Runtime/Exceptions/ExceptionState.cs` — add `public static bool SizeTruncationChecking { get; set; }`
    + `public static void RaiseSizeTruncation(...)` (mirror `ArgumentError` at lines ~177–207).
  - `src/Cobol.Net.Compiler/Binding/Procedure/Verbs/EcBinder.cs` — the `SizeNames` set (line ~275 already
    lists EC-SIZE-TRUNCATION) must be honored at the arithmetic-store bind site for ADD/SUBTRACT/MULTIPLY/DIVIDE/COMPUTE
    receivers, not only receiver-capacity.
  - `src/Cobol.Net.Compiler/CodeGen/Verbs/ArithmeticEmitter.cs` (the `SizeErrEcVar` onFail latch)
    — emit `ExceptionState.LastName = "EC-SIZE-TRUNCATION"` (via `SizeErrEcVar`) when a PROHIBITED store
    is inexact AND `EC-SIZE` checking is on.
- **Change shape:** when the receiver's ROUNDED MODE resolves to PROHIBITED and the scaled result is inexact, the
  store latches EC-SIZE-TRUNCATION (fatal unless TURNed) in addition to the existing ON SIZE ERROR observable. Gate
  the latch on the compile-time `TurnState` (`src/Cobol.Net.Compiler/Binding/TurnState.cs`) for EC-SIZE so no cost
  when unchecked.
- **Verify:** golden `tests/conformance/2023/ec_size_truncation_prohibited.cob` — a COMPUTE with ROUNDED MODE IS
  PROHIBITED on an inexact result under `>>TURN EC-SIZE CHECKING ON`, observing `FUNCTION EXCEPTION-STATUS` =
  `EC-SIZE-TRUNCATION`; plus the receiver unchanged (existing `rounded_mode_prohibited` behavior preserved).
  `dotnet <cli> tests/conformance/2023/ec_size_truncation_prohibited.cob --std 2023 --run` → expected `.out`.
- **Why:** ISO2023 plan §M2-ARITH-1 follow-up #2 explicitly leaves "the named-EC exception object (EC-SIZE-TRUNCATION)
  + USE framework" as future work; this closes it and is the prerequisite for a conforming PROHIBITED.
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — EC-SIZE-TRUNCATION raised on PROHIBITED-inexact arithmetic stores (§14.7.5); ROUNDED MODE PROHIBITED closed`

### STEP 3 — EC-BOUND-OVERFLOW + EC-BOUND-REF-MOD
- **Spec:** §8.5.1.9 (bounds / EC-BOUND-OVERFLOW = Nonfatal), VCR row 30 (§ ref-mod, REF-MOD-ZERO-LENGTH ↔
  EC-BOUND-REF-MOD = Fatal, 2023), §14.6.13.1.6 Table 13. Editions: EC-BOUND-OVERFLOW 2002+ (nonfatal); the
  REF-MOD-ZERO-LENGTH behavior + its `REF-MOD-ZERO-LENGTH` directive is 2023.
- **Files:** `ExceptionState.cs` (`BoundOverflowChecking`, `BoundRefModChecking` flags + raise helpers); the
  subscript/ref-mod emit path (search `RefModPlace` in `src/Cobol.Net.Compiler/CodeGen/` and the OCCURS DYNAMIC grow
  path from P6/P12 — memory `project_greenfield_state` flags the `GrowTo` EC-BOUND-OVERFLOW follow-on).
- **Change shape:** where OCCURS DYNAMIC growth exceeds capacity (§8.5.1.9), raise nonfatal EC-BOUND-OVERFLOW under
  EC-BOUND checking (today it is LOUD but not the named condition). For zero-length ref-mod (`x(p:0)`), raise fatal
  EC-BOUND-REF-MOD *unless* the 2023 `REF-MOD-ZERO-LENGTH` directive is in effect (VCR row 30/109); pre-2023 the
  result stays undefined-but-non-raising (gate by DialectLevel).
- **Verify:** goldens `tests/conformance/2023/ec_bound_overflow.cob` (dynamic table grow past capacity under `>>TURN
  EC-BOUND-OVERFLOW CHECKING ON` → EXCEPTION-STATUS) and `ec_bound_refmod_zero.cob` (zero-length ref-mod → fatal
  EC-BOUND-REF-MOD at 2023; accepted-undefined at 2014). Below-2023 witness for REF-MOD-ZERO-LENGTH directive.
- **Why:** memory `project_greenfield_state` explicitly flags both as LOUD-not-raised follow-ons; the EC surface must
  be closed for the exit criteria.
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — EC-BOUND-OVERFLOW (§8.5.1.9) + EC-BOUND-REF-MOD zero-length ref-mod (VCR 30, 2023)`

---

### WAVE C — 2023 new-feature constructs (each: superset grammar rule + constructs.json row + VersionConformancePass rule → below-2023 COBOLNET0900 + conformance golden)

> **Pattern for every step in this wave (do NOT deviate — per `docs/rearchitecture/DESIGN-version-conformance-pipeline.md`):**
> 1. Add the SUPERSET grammar alternative in the appropriate `Core/*.g4` (or preprocessor for directives), stamped
>    with its committed-match construct-id annotation (or reaching a self-identifying bound node) — NEVER a new
>    parse-time edition predicate (unless a proven load-bearing ambiguity needing a forward-detect) and NEVER a
>    binder-embedded `Check`.
> 2. Add a `constructs.json` row `introducedIn: 2023`, pinned code (0900 raised by the construct's
>    `VersionConformancePass` rule for the below-edition reject; a 15xx code only for a bind-dependent semantic
>    residue).
> 3. Bind → bound node → emit → runtime; all semantics in the binder (memory `feedback_binder_no_ir`); the binder
>    stays edition-agnostic — the edition gate lives ONLY in the `VersionConformancePass` rule.
> 4. Positive golden in `tests/conformance/2023/` (+ manifest entry) AND a negative fixture at EVERY earlier
>    edition proving the COBOLNET0900 diagnostic naming the construct + "requires --std 2023".
> 5. **FULL legacy guard** (shared `.g4` change) — must stay NIST 353 MATCH.

### STEP 4 — Boolean shift operators B-SHIFT-L / B-SHIFT-R / B-SHIFT-LC / B-SHIFT-RC
- **Spec:** §8.8.2 boolean operators; VCR rows 9 (E.2 item 3) + 32 (reserved words) + 46 (E.3.3 item 3). Builds on
  the Phase-10 boolean base (`PIC 1`/`USAGE BIT`, `B"…"` — ISO2023 plan M2-DATA-4).
- **Files:** `Core/CobolLexer.g4` (4 tokens), `Core/CobolExpressions.g4` (boolean-expression tier), the boolean
  renderer `src/Cobol.Net.Compiler/CodeGen/Emit/BooleanRenderer.cs` + `CobolBool` runtime (`src/Cobol.Net.Runtime/Text/CobolBool.cs`).
- **Semantics:** logical shift (L/R) fills vacated positions with boolean 0; circular (LC/RC) rotates over the
  boolean-digit width of the operand (§8.8.2). New reserved words at ≥2023 only (below-2023 the spellings stay
  user-defined words — VCR row 32).
- **Verify:** golden `tests/conformance/2023/boolean_shift.cob` (each of L/R/LC/RC on a known bit string → expected);
  below-2023 negative (the operator token → COBOLNET0900).
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — boolean shift operators B-SHIFT-L/R/LC/RC (§8.8.2, 2023; VCR 9/32/46)`

### STEP 5 — Group SYNCHRONIZED (VCR 43) + NO SIGN (VCR 42)
- **Spec:** §13.18.53 SYNCHRONIZED (group form, E.3.2 item 6); §13.18.60 USAGE `NO SIGN` phrase on PACKED-DECIMAL
  (E.3.2 item 5). Both 2023 new-feature.
- **Files:** `Core/CobolData.g4` (allow `SYNCHRONIZED` on a group level; add `NO SIGN` to the USAGE phrase), the
  `DataBinder` clause decoder (`BindEntry`), `PicInfo`/`NumProfile` for NO SIGN (unsigned packed representation).
- **Semantics:** group SYNCHRONIZED applies as if specified on each permitted contained elementary item (§13.18.53);
  NO SIGN → a PACKED-DECIMAL item with no sign nibble (unsigned magnitude). Below-2023 → COBOLNET0900.
- **Verify:** goldens `group_synchronized.cob`, `packed_no_sign.cob`; below-2023 negatives.
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — group SYNCHRONIZED (VCR 43) + USAGE NO SIGN (VCR 42), 2023`

### STEP 6 — Dynamic-length elementary item SET-to-set-length (VCR 60)
- **Spec:** §14.9.38 SET (dynamic-length form, E.3.3 item 17); depends on the P12 dynamic-length-item data model.
- **Files:** `Core/CobolControlFlow.g4` / the SET grammar (`SET LENGTH OF id …` or `SET id … LENGTH`), the SET binder
  partial, the dynamic-length runtime type from P12.
- **Semantics:** SET assigns the length of a dynamic-length elementary item (§14.9.38). If P12 already added this as
  part of "dynamic length", verify + flip the VCR row; otherwise implement. Below-2023 → COBOLNET0900.
- **Verify:** golden `dynamic_length_set.cob`; below-2023 negative.
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — SET length of a dynamic-length elementary item (VCR 60, 2023)`

### STEP 7 — CONTINUE timed pause + EC-CONTINUE (VCR 57)
- **Spec:** §14.9.9 CONTINUE (`CONTINUE AFTER arithmetic-expression SECONDS`, E.3.3 item 14); EC-CONTINUE family
  (EC-CONTINUE-IMP fatal-imp, EC-CONTINUE-LESS-THAN-ZERO nonfatal — already in the catalog, 2023). A.3 item 8: the
  precision >.99 seconds is processor-dependent.
- **Files:** `Core/CobolControlFlow.g4:258` (`continueStatement` — add the AFTER phrase), a `BoundContinueAfter`
  node, the emitter (a real timed pause — `System.Threading.Thread.Sleep` scaled from the seconds expression),
  `ExceptionState` (`ContinueChecking` + `RaiseContinueLessThanZero`).
- **Semantics:** pause `n` seconds; if `n < 0` raise EC-CONTINUE-LESS-THAN-ZERO (nonfatal) and do not pause;
  precision beyond .99 s is documented processor-dependent (A.3 item 8 → §4.2.6 warning row). Below-2023 → COBOLNET0900.
- **Verify:** golden `continue_after.cob` (a small pause + a negative-seconds → EXCEPTION-STATUS EC-CONTINUE-LESS-THAN-ZERO;
  keep the pause tiny, e.g. 0.01s, so the test is fast). Below-2023 negative.
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — CONTINUE AFTER timed pause + EC-CONTINUE-LESS-THAN-ZERO (§14.9.9, 2023; VCR 57)`

### STEP 8 — PICTURE EDITING phrase (VCR 62)
- **Spec:** §13.18.40 PICTURE `EDITING` phrase (arbitrary-size literal for simple / sign-sensitive fixed insertion,
  E.3.3 item 19). 2023 new-feature.
- **Files:** `Core/CobolData.g4` (PICTURE clause — the EDITING phrase), `PicInfo`/`PictureAnalyzer` (the edited mask),
  the numeric-edited renderer (`src/Cobol.Net.Compiler/CodeGen/Emit/` — `CobolEdit`).
- **Verify:** golden `picture_editing.cob` (an EDITING literal insertion → expected edited output); below-2023 negative.
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — PICTURE EDITING phrase (§13.18.40, 2023; VCR 62)`

### STEP 9 — EXCEPTION-FILE / EXCEPTION-FILE-N optional file-connector argument (VCR 68/69)
- **Spec:** §15.29 EXCEPTION-FILE, EXCEPTION-FILE-N — the optional file-connector argument (E.3.3 items 25/26). 2023.
- **Files:** the intrinsic binder (`Binding/Procedure/Verbs/IntrinsicBinder.cs`) + `IntrinsicCatalog` (add the optional arg),
  `EcFunctions`/`ExceptionState.LastFile` runtime. When the argument is omitted, behavior is unchanged (last-referenced
  connector); when present, report on the named connector.
- **Verify:** golden `exception_file_arg.cob` (two files, force an exception on one, query EXCEPTION-FILE with the
  connector argument). Below-2023: the argument form → COBOLNET0900 (the no-arg form stays legal pre-2023 if it was
  already implemented — verify).
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — EXCEPTION-FILE / EXCEPTION-FILE-N optional connector argument (§15.29, 2023; VCR 68/69)`

### STEP 10 — PERFORM … WHEN exception-checking + PERFORM … UNTIL EXIT (VCR 79/80)
- **Spec:** §14.9.31 PERFORM — the exception-checking WHEN variant (E.3.3 item 36; ties to Table 1 items 19a/19b —
  declaratives now executed) and the `UNTIL EXIT` infinite-loop phrase (E.3.3 item 37). Note
  `Binding/Procedure/Verbs/EcBinder.cs:128` already says the WHEN form "is 2023, a later wave" — this is that wave.
- **Files:** `Core/CobolControlFlow.g4:18` `performStatement` / `performOptions` (add `WHEN condition` +
  `UNTIL EXIT`); the PERFORM binder; the EC engine hook so a matching exception inside the range triggers the WHEN
  imperative (§14.9.31 GR). Below-2023 → COBOLNET0900.
- **Verify:** goldens `perform_when_exception.cob` (a checked exception inside the range routes to the WHEN block) and
  `perform_until_exit.cob` (loop exited via EXIT PERFORM). Below-2023 negatives.
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — PERFORM WHEN exception-checking + PERFORM UNTIL EXIT (§14.9.31, 2023; VCR 79/80)`

### STEP 11 — GOBACK status phrase (VCR 75) — only if not covered by P9
- **Spec:** §14.9.16 GOBACK (`GOBACK … WITH … STOP`-style status phrase, effective only in a main program, E.3.3
  item 32). Verify P9 (OO) did not already land it (GOBACK RETURNING is separate).
- **Files:** `Core/CobolControlFlow.g4` GOBACK; the GOBACK binder; the run-unit termination path
  (`src/Cobol.Net.Runtime/Control/`). Below-2023 → COBOLNET0900.
- **Verify:** golden `goback_status.cob` (main-program GOBACK with a status → process exit code). Below-2023 negative.
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — GOBACK status phrase (§14.9.16, 2023; VCR 75)`

### STEP 12 — Smaller 2023 syntax deltas: WRITE BEFORE+AFTER (VCR 45), SUPPRESS WHEN alt-key (VCR 85), COBOL words 63 chars (VCR 54)
- **Spec:** §14.9.51 WRITE (`BEFORE AND AFTER ADVANCING`, E.3.3 item 2); §13.18.x ALTERNATE RECORD KEY `SUPPRESS WHEN`
  (E.3.3 item 42); §8.x max word length 63 (E.3.3 item 11 — a lexer/validator limit bump gated ≥2023, 30 below).
- **Files:** `Core/CobolIO.g4` (WRITE, SELECT ALTERNATE KEY), the keyed-IO binder + runtime `IndexedFile`; the
  lexer/validator word-length check. Below-2023 → COBOLNET0900 (WRITE/SUPPRESS) / a length diagnostic for >30.
- **Verify:** goldens `write_before_and_after.cob`, `alt_key_suppress_when.cob`, `word_length_63.cob`; below-2023
  negatives.
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — WRITE BEFORE+AFTER, SUPPRESS WHEN alt-key, 63-char words (2023; VCR 45/85/54)`

---

### WAVE D — Compiler directives (COBOL-WORDS / PUSH / POP / DISPLAY / FLAG-14)

### STEP 13 — COBOL-WORDS / PUSH / POP / DISPLAY directives (VCR 55/59/81/11)
- **Spec:** §7.3 compiler directives — `>>COBOL-WORDS` (modify reserved/context/function lists + prohibit user words,
  E.3.3 item 12); `>>PUSH`/`>>POP` (save/restore directive state, E.3.3 item 38); `>>DISPLAY` (compile-time message,
  E.3.3 item 16). These MUTATE the per-unit word lists that Phase 3 made overridable (`EditionInfo`/`ReservedWordSet`
  per-unit seam).
- **Files:** the preprocessor (`src/Cobol.Net.Frontend/Preprocessor/`) — the directive scanner; the per-unit word-set
  override surface from P3/P4 (`Cobol.Net.Editions.ReservedWordSet` per the edition-framework design); a directive
  state-stack for PUSH/POP.
- **Semantics:** `>>COBOL-WORDS EQUATE/SUBSTITUTE/RESERVE/UNDEFINE` mutates the effective word set for the rest of the
  compilation group; `>>PUSH ALL`/`>>POP ALL` snapshot/restore the full directive state; `>>DISPLAY` writes to the
  compile log (a warning-channel line). All 2023; below-2023 → the directive is unrecognized → COBOLNET0900 (or the
  existing "unknown directive" path, made specific).
- **Verify:** golden `cobol_words_directive.cob` (equate a reserved word, use it as a user word, prove the effect);
  `push_pop_directive.cob` (a directive changed then restored). `>>DISPLAY` asserted via the compile-warning capture.
  Below-2023 negatives.
- **Why:** the scope explicitly calls out "mutating the per-unit word lists Phase 3 made overridable" — this is the
  consumer of that seam.
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — COBOL-WORDS / PUSH / POP / DISPLAY directives (§7.3, 2023; VCR 55/59/81/11)`

### STEP 14 — FLAG-14 directive (VCR 64) + retarget FLAG-02 as obsolete
- **Spec:** §7.3.15 FLAG-14 (flag 2014↔2023 incompatibilities, E.3.3 item 21); §7.3.14 FLAG-02 now obsolete (VCR
  rows 91/96/115). FLAG-14 GR4 rows (VCR 102–113) are the per-construct flags matching the Table 1 behavior changes.
- **Files:** the preprocessor directive scanner; the diagnostic sink so a FLAG-14-flagged construct emits the matching
  warning. This wires the Table-5 FLAG-14 rows to the Table-1 behavior rows implemented in Wave I.
- **Semantics:** `>>FLAG-14 ON` makes the compiler warn on every construct whose 2014→2023 behavior differs (the VCR
  102–113 set). FLAG-02 stays supported (a conforming 2023 impl must) but emits an obsolete-flag warning (COBOLNET0903).
- **Verify:** golden `flag_14_directive.cob` (turn FLAG-14 on, use a flagged construct, capture the warning);
  `flag_02_obsolete.cob` (FLAG-02 → 0903 warning).
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — FLAG-14 directive + FLAG-02 obsolete flag (§7.3.14/.15, 2023; VCR 64/91/96/115)`

---

### WAVE E — EXTERNAL run-unit conformance cluster (VCR 15/16/18/31/63 + EC-EXTERNAL)

### STEP 15 — EXTERNAL conformance checking + EC-EXTERNAL
- **Spec:** §13.18.27 EXTERNAL — the 2023 conformance-checking model (E.2 items 9/10/12/24): external items may be
  strongly typed (row 63/16); CONSTANT RECORD only for strongly-typed external items (row 16); a consistent FILE
  STATUS item required across corresponding SELECTs (row 18/12); a relative key must be the same corresponding
  external item (row 31/24); EC-EXTERNAL-* conditions (EC-EXTERNAL-DATA-MISMATCH / -FILE-MISMATCH / -FORMAT-CONFLICT /
  -IMP — all 2023, in the catalog) raised when the description of an external item conflicts across run-unit elements.
- **Files:** `DataBinder` EXTERNAL re-basing (`DataBinder.cs` external/global cell logic), the cross-assembly EXTERNAL
  store (`src/Cobol.Net.Runtime/Control/ExternalStore.cs` from P8), `ExceptionState` (`ExternalChecking`), the file
  model for the FILE STATUS / relative-key consistency checks.
- **Semantics:** at run-unit link time, if two elements describe the same EXTERNAL name incompatibly (data desc / file
  mismatch / format conflict) raise the matching EC-EXTERNAL-* (fatal) when EC-EXTERNAL checking is enabled in BOTH
  elements (VCR 15 — "effective only when enabled in BOTH"). Gate the whole cluster ≥2023.
- **Verify:** golden `external_conformance.cob` (two program units sharing an EXTERNAL item with a deliberate
  description mismatch under `>>TURN EC-EXTERNAL CHECKING ON` in both → EXCEPTION-STATUS EC-EXTERNAL-DATA-MISMATCH);
  a strongly-typed EXTERNAL CONSTANT RECORD positive case (row 16/63). Below-2023: strongly-typed EXTERNAL →
  COBOLNET0900.
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — EXTERNAL run-unit conformance cluster + EC-EXTERNAL-* (§13.18.27, 2023; VCR 15/16/18/31/63)`

---

### WAVE F — EC remnants: USE FOR DEBUGGING + DEBUG-ITEM

### STEP 16 — USE FOR DEBUGGING + the DEBUG-ITEM special register (at --std 85)
- **Spec:** the X3.23-1985 debug facility (USE FOR DEBUGGING declarative; the DEBUG-ITEM register: DEBUG-LINE,
  DEBUG-NAME, DEBUG-SUB-1/2/3, DEBUG-CONTENTS). Removed by 2002 — the gate `use-for-debugging-removed-2002` /
  COBOLNET0902 is already in place (VCR 7.17). Editions: implemented + active only at `--std 85` with the object-time
  debug switch; below the switch the debugging section compiles as comment lines (already the case).
- **Files:** the declaratives binder (`Binding/Procedure/ProcedureTableBuilder.cs`), a `DEBUG-ITEM` special-register data model, the
  PROCEDURE-DIVISION trigger points (paragraph/section entry, statement execution) — emit DEBUG-ITEM population + the
  USE FOR DEBUGGING declarative invocation when the compile-time WITH DEBUGGING MODE switch is on. Today these stage
  COBOLNET0899 (VCR 7.17: "a DEBUG-* register reference under the switch diagnoses 0899 not-implemented").
- **Semantics (§ X3.23-1985 debug):** with WITH DEBUGGING MODE (SOURCE-COMPUTER) present, each USE FOR DEBUGGING
  target's activation populates DEBUG-ITEM and runs the declarative. Keep the leniency documented: the full trigger
  set is scoped to what the DB-series NIST programs exercise (DB101A/DB103M/DB301M–305M).
- **Verify:** move the DB-series NIST programs from golden-less residue toward compiled-and-run where a golden exists;
  golden `tests/conformance/85/use_for_debugging.cob` (a USE FOR DEBUGGING on a paragraph, DEBUG-NAME/DEBUG-LINE
  observed). Confirm `use-for-debugging-removed-2002` still rejects ≥2002.
- **Why:** the scope names "USE FOR DEBUGGING + DEBUG-ITEM" as an EC-remnant; it retires the largest 0899 staging in
  the 85 corpus.
- **Legacy guard:** binder-only if the grammar already parses USE FOR DEBUGGING (it does — row 7.17); run the FULL
  legacy guard anyway since DB-series NIST programs are in the corpus.
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — USE FOR DEBUGGING + DEBUG-ITEM register at --std 85 (X3.23-1985 debug facility); DB-series de-staged`

---

### WAVE G — Obsolete / archaic flags (§4.2.12 / §4.2.13) — 0903 warnings

### STEP 17 — CALL … ON OVERFLOW removal (VCR 3) + the remaining flag-obsolete rows
- **Spec:** VCR row 3 (CALL … ON OVERFLOW removed 2023 — gate-behavior-by-dialect, accept pre-2023); row 28
  (FLAG-85 / FLAG-NATIVE-ARITHMETIC / Standard Arithmetic removed — the non-MOVE-QUOTE legs still TODO); rows
  93/116 (STANDARD-BINARY / SBIDI obsolete); the four archaic VCR rows 89/90/126/127 already GATED (verify) — "the
  four archaic VCR rows get their own 0903 sub-code" means give each a distinct `constructs.json` row id under
  COBOLNET0903.
- **Files:** the CALL binder (`Binding/Procedure/Verbs/CallBinder.cs` — ON OVERFLOW arm; accept pre-2023, COBOLNET0902 at 2023);
  the OPTIONS/ARITHMETIC binder (`OptionsBinder`) for STANDARD-BINARY (0903 obsolete flag); `constructs.json` rows.
- **Verify:** negatives/goldens: `call_on_overflow_2023_removed.cob` (0902 at 2023, accepted at 2014);
  `standard_binary_obsolete.cob` (0903 warning). Flip VCR rows 3/28/93/116 status.
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — CALL ON OVERFLOW removal + STANDARD-BINARY/SBIDI obsolete flags (VCR 3/28/93/116)`

---

### WAVE H — A.3 disposition sweep + §4.2.6 warnings + user documentation (§4.2.16)

### STEP 18 — One-pass A.3 processor-dependent disposition sweep (46 items)
- **Spec:** §A.3 (the ~46-item processor-dependent language element list, `specs/ISO_COBOL.md:40052`), read against
  §4.2.6 (processor-dependent — "provide a warning mechanism at compile time to indicate use of syntactically-
  detectable processor-dependent language elements not supported") and §4.2.7 (optional elements) / §4.2.16 (user
  documentation).
- **Do:** For each A.3 item, decide one of: **supported** (implemented — no action beyond recording); **not
  supported → §4.2.6 warning** (emit a compile-time warning naming the element when syntactically detected;
  executable code not required); **N/A** (feature not in the language surface). Write the disposition to the new
  `docs/CONFORMANCE.md` (§A.3 table) — this IS the "one-pass A.3 disposition sweep" and satisfies the §4.2.16 user-
  documentation obligation. Wire the "not supported → warning" items to a single `COBOLNET1560`-band processor-
  dependent-not-supported warning routed through the diagnostic sink (singular pattern). Examples from A.3:
  item 2/3 STANDARD-BINARY/STANDARD-DECIMAL arithmetic (not supported → warning, already obsolete); item 4
  asynchronous messaging (not supported → §4.2.6 warning, feeds Step 20); item 6/7 commit/rollback (not supported →
  warning, Step 20); item 8 CONTINUE precision >.99 s (supported-with-documented-limit, Step 7); item 11/… I-O
  status '37' (supported per Step 19); the FLOAT-BINARY/DECIMAL / endianness items (per P12 IEEE work — supported or
  warned).
- **Verify:** golden(s) that each "not-supported detectable" element triggers the §4.2.6 warning (e.g.
  `a3_standard_binary_warning.cob`); `docs/CONFORMANCE.md` reviewed for completeness (all 46 rows present).
- **Why:** exit criterion / §4.2.16 requires the documentation to exist; §4.2.6 requires the warning mechanism.
- **COMMIT BOUNDARY.** `docs+feat(cobolnet): Phase 13 — A.3 processor-dependent disposition sweep + §4.2.6 warning mechanism + docs/CONFORMANCE.md (§4.2.16)`

---

### WAVE I — Table 1/5 behavior-row burn-down (~44 rows)

### STEP 19 — I-O status behavior rows (VCR 21/22/23/24/78 + DELETE FILE statuses)
- **Spec:** VCR rows 21 (I-O '04' clarified + I-O-STATUS-04 directive), 22 ('07' restricted to OPEN/CLOSE), 23 ('0x'
  case equivalence implementor-dependent), 24 ('37' insufficient authority on OPEN); row 78 (DELETE FILE may set
  '05','37','39','41','62'). §9.1.13 I-O status. Editions: each gated 2014→2023 (or documented implementor-dependent).
- **Files:** the runtime file connectors (`src/Cobol.Net.Runtime/IO/` — Sequential/Relative/Indexed), the DELETE FILE
  runtime (from the DELETE FILE step), `ExceptionCatalog.IoEcOfStatus`; the I-O-STATUS-04 directive (Wave D pattern).
- **Do:** implement the status settings at ≥2023 (gate by DialectLevel where "Affects existing? = Yes"); document the
  implementor-dependent ones (row 23 → §4.2.6 note in CONFORMANCE.md). Wire the DELETE FILE statuses (residual from
  the DELETE FILE step).
- **Verify:** goldens exercising each new status (e.g. `delete_file_status_41.cob` — DELETE FILE of an open file →
  '41'); the I-O-STATUS-04 directive golden.
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — I-O status behavior rows '04'/'07'/'0x'/'37' + DELETE FILE statuses (§9.1.13, 2023; VCR 21/22/23/24/78)`

### STEP 20 — VALUE-clause numeric-edited conformance rows (VCR 34/35/36/86 + Table 5 FLAG-14 twins)
- **Spec:** VCR rows 34 (VALUE literal categories checked for numeric-edited, E.2 item 27), 35
  (figurative ZERO for numeric-edited = numeric zero, NUM-ED-ZERO-FIG-CONSTANT), 36 (editing symbols required/
  auto-supplied), 86 (numeric-literal VALUE permitted for numeric-edited, E.3.3 item 43); §13.18.63 VALUE. The
  matching FLAG-14 rows 107/110/112 warn.
- **Files:** the VALUE-clause binder (`DataBinder` VALUE conformance), `PicInfo`/`CobolEdit` (edited materialization).
- **Do:** at ≥2023 check the literal category against PIC/USAGE (row 34), treat figurative ZERO as numeric zero →
  edited per PICTURE (row 35), require/auto-supply editing symbols (row 36), permit a numeric-literal VALUE on a
  numeric-edited item (row 86). Gate by DialectLevel (pre-2023 behavior preserved). Wire the FLAG-14 twins.
- **Verify:** golden `value_numeric_edited_2023.cob` (a numeric-literal VALUE + figurative ZERO on numeric-edited →
  expected edited init); below-2023 witness.
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — VALUE-clause numeric-edited conformance rows (§13.18.63, 2023; VCR 34/35/36/86)`

### STEP 21 — Remaining Table 1 behavior rows (MERGE row 27, transfer-of-control sections row 33, WRITE EOP row 37, EVALUATE-directive row 14, ALL-with-unspecified-length row 17, case-mapping rows 20/49, leap-year row 13)
- **Spec:** the residual "Affects existing? = Yes" Table 1 rows not covered above. For each: gate the 2014→2023
  behavior by DialectLevel, or (for "Affects existing? = No" clarification rows) pin-to-spec with a recorded
  determination.
- **Files:** per-row — MERGE binder (`Binding/Procedure/Verbs/SortBinder.cs`, prohibit MERGE in a MERGE output / file-SORT proc at
  2023), the procedure-table transfer-of-control check (`Binding/Procedure/ProcedureTableBuilder.cs` — include sections, row 33),
  WRITE binder (END-OF-PAGE fallthrough, row 37), the `>>EVALUATE` directive (row 14), figurative-ALL length
  (row 17), UPPER-/LOWER-CASE case mappings (rows 20/49 — `CobolIntrinsics.Text`), leap-year (row 13 — no gating
  needed).
- **Do:** work the rows in a single sweep; each row either lands a gated behavior + golden, or gets a one-line
  pin-to-spec / no-op disposition in the VCR with a citation (rows 13/50/51/52/82/83/84/87/88/130 are "Affects
  existing? = No" → `done (pin-to-spec)` or `none`).
- **Verify:** one golden per gated row (e.g. `merge_in_output_procedure_rejected.cob`, `transfer_control_section.cob`,
  `write_eop_no_phrase.cob`); flip every touched VCR row.
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — Table 1 residual behavior rows dispositioned/gated (VCR 13/14/17/20/27/33/37/49/…)`

---

### WAVE J — Documented non-support diagnostics (MCS / commit-rollback / VALIDATE / screen)

### STEP 22 — Documented-non-support diagnostics for the four non-goal facilities
- **Spec:** MCS asynchronous messaging (VCR 38, §A.3 item 4 — processor-dependent → §4.2.6); commit/rollback (VCR 39,
  §A.3 items 6/7 — processor-dependent); VALIDATE (VCR 95/117–125/129 — obsolete-optional §4.2.13/§4.2.7); screen
  (§13.x SCREEN SECTION — optional §4.2.7). None are project goals.
- **Files:** the grammar must *recognize* these constructs enough to name them (or the binder detects the entry
  keyword) and emit a specific diagnostic: VALIDATE/screen → an optional-not-supported warning or error per category;
  MCS/commit-rollback → a §4.2.6 processor-dependent-not-supported warning (executable code not required). Route
  through the single `COBOLNET1560`-band not-supported diagnostic. Add each to `docs/CONFORMANCE.md` (§4.2.7 optional
  / §4.2.13 obsolete lists) — the §4.2.16 obligation.
- **Do:** replace any generic COBOLNET0899 / silent-drop for these four with the specific, named non-support
  diagnostic. Do NOT implement the facilities.
- **Verify:** negatives `mcs_not_supported.cob`, `commit_not_supported.cob`, `validate_not_supported.cob`,
  `screen_not_supported.cob` — each → the specific non-support diagnostic naming the facility + citing §4.2.6/§4.2.7/
  §4.2.13. `docs/CONFORMANCE.md` lists all four.
- **COMMIT BOUNDARY.** `feat(cobolnet): Phase 13 — documented non-support diagnostics for MCS / commit-rollback / VALIDATE / screen (§4.2.6/§4.2.7/§4.2.13)`

---

### WAVE K — Matrix closure & VCR status update

### STEP 23 — VCR + version-matrix + diagnostic-map sync
- **Do:**
  - Flip EVERY VCR Table 1/2/3/5 row this phase touched to its final `GATED`/`done`/`documented-non-support` status
    with the DEVLOG # + code (no bare `TODO` left for a 2014→2023 row — exit criterion 1).
  - Add the per-construct rows to `tests/version-matrix/constructs.json` (introduction gates) and add the
    behavior-variant matrix cells for the gated behavior rows (run-under-each-`--std`-and-diff, per
    `docs/VERSION_TEST_MATRIX_DESIGN.md`).
  - Update the diagnostic-code map: the 15xx band now runs to its new head; record 1539… and the 1560 non-support
    band in `docs/COBOLNET_DESIGN.md` + the resume-prompt STATE banner.
  - Update `docs/ISO2023_CONFORMANCE_PLAN.md` (tick the M4 items), `docs/DOC_INDEX.md` (the new `docs/CONFORMANCE.md`
    row), and `resume-prompt.md` STATE.
- **Verify:** a drift test (if present, `CorpusManifestTests`/`constructs.json` drift) green; grep the VCR for
  `| TODO |` on any 2014→2023 row → expect none (or an explicit documented-non-support disposition).
- **COMMIT BOUNDARY.** `docs(cobolnet): Phase 13 — VCR Tables 2/3/5 dispositioned, version matrix closed, diagnostic map + resume-prompt synced`

### STEP 24 — Adversarial find→verify review over the whole phase
- **Do:** Every prior feature's review found real defects (memory: run it). Launch a find→verify review (the proven
  cadence — one reviewer per wave: EC-store, 2023-constructs, directives, EXTERNAL, behavior-rows, non-support) over
  the phase's commits against `specs/ISO_COBOL.md`. Confirm each finding by reproduction on the prebuilt CLI, fix all
  CONFIRMED defects, add a regression test per fix. Watch specifically for: silent EC non-raise (verify-by-RUNNING,
  not by reading the guard), a below-edition gate that false-rejects a legal pre-2023 use, a shared-`.g4` change that
  skipped the full legacy guard, and a 0903/0900 mis-code.
- **Verify:** full greenfield battery + FULL legacy guard green; the review's confirmed-defect count → 0 open.
- **COMMIT BOUNDARY.** `fix(cobolnet): Phase 13 adversarial review — N confirmed defects, all fixed`

---

## 4. Diagnostic-code allocation (suggested — pin contiguously as steps land)

Band head at phase start: **`COBOLNET1538`** (15xx feature band). New introduction gates for grammar-gated
constructs use **`COBOLNET0900`** raised by the construct's `VersionConformancePass` rule (they enter the matrix);
obsolete/archaic flags use
**`COBOLNET0903`**. The 15xx allocations below are for bind-dependent semantic residue / non-support:

| Suggested code | Step | Purpose |
|---|---|---|
| (0900 via the pass) | 4–15 | below-2023 introduction reject for each new construct |
| 1539 | 7 | CONTINUE AFTER precision-limit / EC-CONTINUE bind residue |
| 1540 | 15 | EXTERNAL conformance semantic mismatch (bind-detectable) |
| 1541 | 16 | USE FOR DEBUGGING residual-leniency staged note (replaces the 0899 staging) |
| 1542–1544 | 19/20/21 | behavior-row bind residues (I-O status, VALUE, MERGE/transfer) |
| 1560 | 18/22 | processor-dependent / optional NOT-SUPPORTED (§4.2.6/§4.2.7) — the ONE non-support diagnostic |

> The executing session assigns the ACTUAL numbers contiguously from the current head as each lands, and records
> them in the diagnostic-code map. The table is guidance, not a contract.

## 5. Verification (phase-end)

Run the FULL battery and the neutrality checks:
1. **Greenfield conformance:** `dotnet test E:/CobolSharp/tests/Cobol.Net.Tests.Conformance` — green, and the new
   `tests/conformance/2023/` goldens are all discovered + passing (check `manifest.json` has zero `pending` for the
   implemented constructs).
2. **Unit:** `dotnet test E:/CobolSharp/tests/Cobol.Net.Tests.Unit` — green.
3. **FULL legacy guard:** `bash scripts/guard.sh` — NIST 353 MATCH, 0 regressions (this proves the shared-`.g4`
   changes are 85-byte-safe — behavior-neutral on the legacy corpus, per DESIGN-test-build-ci gate 1).
4. **Behavior-neutrality (gate 3):** the emitted-C# characterization snapshots (once P0/P1 stood them up) show no
   diff on unchanged constructs; any intentional emit change is reviewed + re-baselined (COBOLNET_UPDATE_SNAPSHOTS=1
   locally, never in CI).
5. **Below-edition matrix:** for each new 2023 construct, `dotnet <cli> <case>.cob --std 2014 --run` (and 2002/85)
   → the specific COBOLNET0900 diagnostic naming the construct, NOT a generic parse error and NOT a silent accept.
6. **EC observation:** each EC golden, when run at `--std 2023` under `>>TURN … CHECKING ON`, prints the raised
   condition via `FUNCTION EXCEPTION-STATUS` / `EXCEPTION-FILE`; when TURN is OFF, the nonfatal ones do not fire.
7. **VCR completeness:** `grep -c '| TODO |' docs/VERSION_CHANGE_REFERENCE.md` on the 2014→2023 rows → 0
   (every such row is GATED / done / documented-non-support).

## 6. Rollback / resumability

- **Resume anchor:** the STATUS line at the top + `docs/rearchitecture/PHASE-13-audit.md` (Step 1). The audit table's
  "Action this phase" column IS the worklist; a resuming session reads it, finds the last GREEN commit boundary
  (`git log --oneline | grep "Phase 13"`), and continues at the next unstarted step.
- **Each step is an independent commit boundary** leaving the battery green — safe to stop after any step. A
  half-finished step: `git stash` or `git checkout -- <files>` to return to the last green boundary; the audit table
  says what was intended.
- **Risks & mitigations:**
  - *Re-implementing landed work* → Step 1 audit is mandatory and gates all of Wave C.
  - *Shared-`.g4` change breaks the legacy corpus* → run `scripts/guard-fast.sh` after EVERY grammar edit; the
    full `guard.sh` at the commit boundary. A new superset alternative must be byte-invariant at 85 (the
    XOR/OCCURS-DYNAMIC precedent — memory `project_greenfield_state`).
  - *Silent EC non-raise (false-green)* → verify-by-RUNNING every EC golden (the OCCURS-DYNAMIC review lesson: test
    the actual behavior, not just that the guard is green).
  - *Below-edition gate false-rejects a legal pre-2023 use* → every new construct ships BOTH the positive 2023 golden
    AND the below-2023 negative; also spot-check that the spelling is still a legal user-defined word pre-2023 (VCR
    reserved-word rows).
  - *EC-SIZE-TRUNCATION regressing existing ON SIZE ERROR* → Step 2 must preserve the receiver-unchanged behavior of
    `rounded_mode_prohibited`; keep that golden in the battery.

## 7. ISO feature work in this phase — spec sections, editions, conformance goldens

All work targets **COBOL-2023** (DialectLevel 2023) with below-edition rejection at 2014/2002/85, EXCEPT:
- **USE FOR DEBUGGING + DEBUG-ITEM** (Step 16) — a **COBOL-85** facility (removed ≥2002; gate already in place).
- **A.3 sweep / non-support** (Steps 18/22) — conformance-documentation obligations (§4.2.6/§4.2.7/§4.2.13/§4.2.16),
  edition-invariant.

**Spec sections by step:** §14.7.5 + Table 13 (Step 2); §8.5.1.9 + §14.9.x ref-mod (Step 3); §8.8.2 (Step 4);
§13.18.53 + §13.18.60 (Step 5); §14.9.38 (Step 6); §14.9.9 + A.3 item 8 (Step 7); §13.18.40 (Step 8); §15.29
(Step 9); §14.9.31 (Step 10); §14.9.16 (Step 11); §14.9.51 + §8.x (Step 12); §7.3 directives (Steps 13/14);
§13.18.27 (Step 15); X3.23-1985 debug (Step 16); §14.9.7-CALL + §8.8.1-arith obsolete (Step 17); §A.3 + §4.2.6
(Step 18); §9.1.13 (Step 19); §13.18.63 (Step 20); §14.9.28-MERGE / §14.6 transfer / §14.9.51-WRITE / §7.3-EVALUATE
(Step 21); §4.2.6/§4.2.7/§4.2.13 (Step 22).

**Conformance goldens to add** (all in `tests/conformance/2023/` unless noted; each auto-discovered by
`ConformanceTests`, listed in `manifest.json`, and paired with a below-edition negative):
`ec_size_truncation_prohibited`, `ec_bound_overflow`, `ec_bound_refmod_zero`, `boolean_shift`, `group_synchronized`,
`packed_no_sign`, `dynamic_length_set`, `continue_after`, `picture_editing`, `exception_file_arg`,
`perform_when_exception`, `perform_until_exit`, `goback_status`, `write_before_and_after`, `alt_key_suppress_when`,
`word_length_63`, `cobol_words_directive`, `push_pop_directive`, `flag_14_directive`, `flag_02_obsolete`,
`external_conformance`, `use_for_debugging` (in `tests/conformance/85/`), `call_on_overflow_2023_removed`,
`standard_binary_obsolete`, `a3_standard_binary_warning`, the DELETE FILE status cases, `value_numeric_edited_2023`,
`merge_in_output_procedure_rejected`, `transfer_control_section`, `write_eop_no_phrase`, and the four non-support
negatives `mcs_not_supported` / `commit_not_supported` / `validate_not_supported` / `screen_not_supported`.

**Editions validated per construct (the "N per-edition compilers" obligation):** each new-feature construct is
introduced at 2023 (accepted 2023; COBOLNET0900 at 2014/2002/85); each behavior-change row emits the OLDER behavior
below its introducing edition and the NEWER behavior at ≥it (gate by DialectLevel — never hard-code one behavior);
the archaic/obsolete flags warn (COBOLNET0903) only at the edition the spec designates the element archaic/obsolete.
