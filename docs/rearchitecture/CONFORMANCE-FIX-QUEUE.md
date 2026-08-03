# Verified Conformance Fix-Queue (spec-first)

> **The audit-surfaced candidates, INDEPENDENTLY VERIFIED against the spec + code (`wf_29a15db2`).** Each CONFIRMED
> item carries a decision-complete spec-derived FIX and a SPEC-DERIVED golden (expected value computed from the spec,
> never copied from the legacy). Work top-down by severity; land each fix WITH its golden under a comprehensive gate.
> Source ledgers: `CODE-SPEC-AUDIT.md` (CA*), §24 (V54–V59). Part of the P14 full spec-conformance review
> (`DESIGN-spec-conformance-review.md`).

**46 total (43 CONFIRMED + 1 REFUTED + 2 OWNER-DECIDED) · 1 refuted · 0 pending · 45 LANDED · 1 REFUTED · 0 REMAIN (2026-07-29).** *(**⛔ THE 46-FINDING AUDIT IS CLOSED — V59 landed 2026-07-29 across DEVLOG 1097–1102, the last of the 45.** The count previously moved by TWO: CA14 landed, and CA11 · oo was already landed by DEVLOG 1088 with only its heading left unflipped — the duplicate CA11 id, one exceptions-ec and one oo, is what hid it.)* **What remains in this file is NOT the audit: it is the DISCOVERED-during-implementation set — DA2 and DA3, both OPEN, both MAJOR, both cases where CONFORMING SOURCE is rejected at run time. They are now the top of the queue.** *(+ the DISCOVERED-during-implementation candidate DA1 — §12.3.7 hex-literal ALPHABET decode — now ✅ LANDED (DEVLOG 1019); it was not part of the original 46 audit set.)* Original severity mix: blocker=2, major=30, minor=10, nit=2, plus the 2 owner-decided (CA14 major, V59 major-L). Both blockers are done; the 3 remaining are all major/minor (the EC-infra + OO super-batch) + the 2 owner-decided.

**LANDED (spec-first, this campaign):** CA31 ✅, CA32 ✅ (blockers; DEVLOG 995) · CA1 ✅, CA2 ✅ (accept-display-misc; DEVLOG 996) · CA27 ✅, CA28 ✅ (move-convert — CA28 also RETRACTED a spec-wrong test + VCR row 130c; DEVLOG 998) · CA13 ✅, CA39 ✅ (editions-gating; DEVLOG 999) · CA15 ✅, CA16 ✅ (files-io — line-seq over-length '06', OPTIONAL I-O create '05'/'10'; DEVLOG 1000). **+ CA24 ✅ · V54 ✅ · CA23 ✅ · CA25 ✅ (intrinsics batch COMPLETE — EXP/EXP10 overflow + LOG/LOG10 domain; MAX/MIN national category; MAX/MIN/ORD PCS collation; UPPER/LOWER/REVERSE national category; DEVLOG 1001–1004). **+ CA33 ✅ (picture digit-position CAP; DEVLOG 1005) · CA34 ✅ (numeric VALUE range/sign §13.18.63.3 SR2/SR3, new COBOLNET1625; DEVLOG 1006) · CA35 ✅ (USAGE BINARY/COMP/PACKED-DECIMAL requires a numeric picture §13.18.60.3 SR3, reused COBOLNET0881; DEVLOG 1007) · CA4 ✅ (ADD/SUBTRACT-GIVING composite excludes the resultants §14.9.2.3/§14.9.44.3 SR1b; DEVLOG 1008) · CA5 ✅ (ROUNDED/PROHIBITED bind to the final transfer only — `_outermost` flag; DEVLOG 1009) · CA6 ✅ (binary-N operands excluded from the composite §14.7.7 rule 2b; DEVLOG 1010 — arithmetic batch COMPLETE) · CA7 ✅ (a class condition on a zero-length operand is FALSE §8.8.4.4.4 GR1; DEVLOG 1011) · CA36 ✅ (SEARCH range-EC dispatch to a USE declarative when AT END absent §14.9.37.4 GR1b2; DEVLOG 1012). **+ the phase-14 INDEPENDENT-MINORS batch (8 items separated from the EC-infra/OO super-batch; re-scout `wf_a09670d5-cdc`): CA17 ✅ (files-io — a sequential indexed REWRITE's prime-key change-detection is COLLATING-SEQUENCE-based per §14.9.35 GR22 / §12.4.5.12.4 GR1, not ordinal; DEVLOG 1013) · CA8 ✅ (conditions — a bare standard-float SIGN condition is Format 2 §8.8.4.7.3 SR2, tests the IEEE sign bit §8.8.4.7.4 GR2: +0.0 IS POSITIVE / −0.0 IS NEGATIVE; DEVLOG 1014) · V56 ✅ (conditions — a float relation under STANDARD-DECIMAL compares in SDIDI not native double §8.8.4.2.4; DEVLOG 1014) · CA3 ✅ (accept-display — a bare HIGH-/LOW-VALUE in DISPLAY renders the PROGRAM COLLATING SEQUENCE extreme, not the native pin §8.3.3.6.4 GR6/GR7; DEVLOG 1015) · CA19 ✅ + CA20 ✅ (inspect-string — UNSTRING receiver SR4 + sender SR2 category screens §14.9.48.3, runtime-loud per the STRING-side convention; DEVLOG 1016) · CA18 ✅ (files-io — a line-sequential REWRITE overwrites in place per §14.9.35.4 GR17 [00/44/71], no longer a blanket '30'; a delimiter-aware line reader tracks the byte anchor; DEVLOG 1017) · CA26 ✅ (intrinsics — the alphanumeric repertoire is UNICODE [established design]; CHAR/ORD/collation span the full UTF-16 range under a non-native PCS §15.15.3/§12.3.7.4 GR7 1.3, no longer 8-bit-aliased; DEVLOG 1018).** Remaining: 16 fix-ready.** **The phase-14 INDEPENDENT-MINORS batch is COMPLETE (8/8): CA17/CA8/V56/CA3/CA19/CA20/CA18/CA26 all landed.** The 16 remaining are all the bigger/coordinated items: the EC-infra + OO SUPER-BATCH (CA9/10/11/12/V57 · CA21/22/V58 · CA29/30/V55 + CA37/38) and CA14 + V59 (owner-decided). *(DA1, the discovered candidate, is now ✅ LANDED — DEVLOG 1019.)* *(Legacy `GreenfieldOnly` exclusions no longer required — owner decision, DEVLOG 997.)*

**⛔ THE PHASE-B TRACEABILITY REVIEW IS NOW THE SOURCE OF WORK, AND IT HAS OPENED NINE ITEMS: `PB1`–`PB9`
LANDED (2026-07-30).** The older campaigns are closed — the 46-finding audit (45 landed, 1 refuted)
and the discovered set DA1–DA7 — so everything live in this file is a PB item plus the NAMED PARTIAL residue each
landed fix left behind. Nothing is silently deferred: every residue is a row in the traceability inventory.

**⛔ THE TALLY: PB1–PB11 + PB13 + PB19 + PB20 LANDED · PB16 RETIRED · PB12 HALF LANDED · PB14, PB15, PB17, PB18, PB21–PB27 OPEN.**
The queue emptied at PB9 and was refilled by the §15.32–15.44 batch, which is the design working: adjudicating a
clause OPENS items, fixing them CLOSES them, and an empty queue means "adjudicate the next clause".
**There is no BLOCKER open** — PB13, the silently saturating quantizer, landed 2026-08-02. Each half-landed item
names its own remaining half in its entry, and plan §0's NEXT table carries them in the order to work them. PB9's own entry is the standing warning about a MEASURED scope: "exactly one word" came
from sweeping the wrong set, and the real answer was four.

**⚠ PB13 IS THE STANDING WARNING ABOUT A QUEUE ENTRY'S OWN *RECIPE*** (PB8's is about its root CAUSE). This
entry's recipe read "the fix is TWO-SIDED … the runtime must stop silently saturating, which no emitter-side
choice can reach." That conclusion followed from taking the only emitter lever to be the working SCALE. The
actual lever was **whether to quantize at all**, and once used, the whole fix is emitter-side and the runtime
needed no change — the saturation became loud *by construction*. The entry also cited **§14.7.4** for "an
intermediate overflow is a size-error condition"; §14.7.4 is the **ROUNDED phrase**, and the real clause is
**§14.7.5 case 5**. Both errors were INHERITED into the plan and into a runtime doc comment before anyone
re-derived them (CLAUDE.md rule 1 — a citation you did not `--check` is not a citation).

**⚠ PB8 IS THE STANDING WARNING ABOUT A QUEUE ENTRY'S OWN ROOT-CAUSE CLAIM.** Its entry named a LEXER-MODE defect
and budgeted "the riskiest category in this codebase"; a token dump showed the parens were already in DEFAULT mode
and the lexer was never touched. The entry had been REASONED from the lexer source rather than MEASURED. When an
entry names a root cause, re-measure it before budgeting for it — `use_antlr_tree_dump` exists for this.

**TWO OF THE SEVEN WERE BLOCKERS, AND BOTH WERE SILENT** — the reason this review exists rather than a
differential:
· **PB5** — the float→fixed quantizer saturated at |value| ≈ 9.2 × 10⁹, so `FUNCTION ANNUITY(1e10 1)` into an
  ordinary `PIC 9(12)V99` money field returned 9223372036.85 for 10000000001.00, with **NO SIZE ERROR**. Wrong
  arithmetic in routine business ranges.
· **PB7** — every ZERO-ARGUMENT intrinsic was unreachable in the keyword-omitted form: the program COMPILED
  CLEAN and threw at run time.

**⚠ THE COUNT OF FINDINGS IS NOT THE COUNT OF DEFECTS, and reading it that way wastes a session.** The first
batch produced 42 open rows over 11 functions; **31 of them were three root causes** (PB1's dead argument-class
table, PB2's floating-point argument path, PB3's collating tail). The second batch's 41 were **34 of one thing** —
PB1's table simply did not yet list those functions. Cluster before triaging.
## 🧭 FOUND BY THE PHASE-B TRACEABILITY REVIEW (the inventory is now feeding this queue, as designed)

### 📦 BATCH 4 — §15.45–15.57 (2026-08-02): 77 rules, 13 subjects, **ZERO CONFORMS**

> **26 agents, 0 failures, 16 verdicts overturned by the refuters — every overturn a downgrade, as in all three
> prior batches.** Totals: DIVERGES 25 · PARTIAL 16 · NOT-IMPLEMENTED 36 · **CONFORMS 0**. Adjudicated 253 → 330.
> ⚠ **THE FINDING COUNT IS NOT THE DEFECT COUNT** (the standing lesson): 33 of the 36 NOT-IMPLEMENTED are ONE
> owner-ratified disposition — the §A.4.9 locale module, loudly rejected by `COBOLNET1518` — and 20 of the rest
> are ONE root cause, PB1's designed table residue. The genuinely new defects are PB19–PB27 below.
>
> ⛔ **TWO PROCESS TRAPS THIS BATCH EXPOSED, both caught by a mechanism rather than by a person:**
> · **The playbook's own documented merge command would have REVERTED PB11.**
>   `record_verdicts.py scratchpad/phase-b/out-*.json` globs EVERY out-file, including the previous batch's —
>   26 files, 144 records, and four rows re-adjudicated BACKWARDS (`CONFORMS → NOT-IMPLEMENTED` on exactly the
>   FORMATTED-* rows PB11 closed hours earlier, from files dated 07-30 written before PB11 existed). **The tell
>   was the GAP going UP** (3799 → 3803) in the `--dry-run`. Merge the batch's OWN files by name; the glob is a
>   trap the moment a second batch shares the directory.
> · **The inventory gate caught 15 unresolved `test-ref`s** — agents wrote the corpus-golden form
>   `conformance:` for xUnit METHODS. All seven referents existed; only the form was wrong
>   (`conformance-test:<Class>.<Method>` for the Conformance assembly). Fixed in the BATCH FILES and re-merged —
>   nobody hand-edits the inventory.

### PB19 · [MAJOR] · intrinsics · ✅ LANDED 2026-08-02 (DEVLOG 1147) — nine rows, and the classifier under them was lossy
> **Nine rows, each read at its own clause and `--check`ed.** `'i'` INTEGER-OF-DATE/-OF-DAY · `'n'` INTEGER-PART,
> LOG, LOG10 · `'s'` LOWER-CASE, UPPER-CASE, INTEGER-OF-FORMATTED-DATE · **`'b'` INTEGER-OF-BOOLEAN, on a class
> arm that did not exist**. INTEGER-PART is the telling one: its §15.49.3 r1 is the SAME SENTENCE as INTEGER's
> and FRACTION-PART's, both already registered — the rule was enforced for two of three identical functions.
> ⛔ **THE PB20 ORDERING WAS PROVED, NOT ASSUMED:** the pre-PB20 rule was temporarily restored and
> `FUNCTION INTEGER-OF-BOOLEAN(BITS(1:6))` — Annex D's own worked example — was confirmed REJECTED under it.
> ⛔ **AND THE PB1 TRAP SPRANG A SECOND TIME, ONE LAYER DOWN.** The full corpus returned 2 failures on LEGAL
> source (`INTEGER-OF-BOOLEAN(B"00000101")` in `2002/intrinsics_boolean_conv`, and a spec-derived unit test).
> The ROW was right; `ClassOf` flattened every `BoundStringLiteral` to alphanumeric, **ignoring the `Category`
> the record carries** (National for `N"…"`, Boolean for `B"…"`). **A screen is only as good as its classifier,
> and a classifier that flattens a category is how a correct rule rejects a correct program** — PB1's disaster
> was unaudited ROWS; this was an audited row over a lossy CLASSIFIER, invisible to any review of the rows.
> The nested-function shape was already correct (`IntrinsicType.Boolean` → `ResultCategory`) and was left alone.
> ⚠ **RESIDUE, not rounded up:** LOWER-CASE/UPPER-CASE keep the unenforced "at least one character position"
> half (PARTIAL, like REVERSE's `AR-15.78.3-1`), and §15.48.3 r3's "same type as argument-1" is a CROSS-ARGUMENT
> rule a per-position screen cannot express (PARTIAL, like `AR-15.79.3-3`).
> Goldens: `pb19_argument_class_batch5` (incl. the Annex D ref-mod shape) + three negative fixtures.
> The finding as originally recorded follows.

### PB19 (as found) · the §15.3 argument-class screen is absent for THIS batch's functions
> **20 rows over 8 functions — PB1's DESIGNED residue, now due for §15.45–15.57.** `IntrinsicArgumentRules.Verified`
> is deliberately partial and grows as the review adjudicates each clause, so absence is a real finding.
> Unscreened: **INTEGER-OF-DATE** (§15.46.3 r1) · **INTEGER-OF-DAY** (§15.47.3 r1) · **INTEGER-PART** (§15.49.3 r1
> — while the adjacent INTEGER and FRACTION-PART, whose rule text is identical, ARE registered) · **LOG**
> (§15.55.3 r1) · **LOG10** (§15.56.3 r1) · **LOWER-CASE** (§15.57.3 r1 — the same sentence verbatim as REVERSE's
> §15.78.3 r1, which IS registered) · **INTEGER-OF-FORMATTED-DATE argument-2** (§15.48.3 r3).
> ⚠ **TWO ARE NOT ONE-LINE ROWS, and that is the finding:**
> · **INTEGER-OF-BOOLEAN** (§15.45.3 r1) needs a **class-BOOLEAN kind that `Admissible` cannot express** — there
>   is no arm admitting `CobolClass.Boolean` at all.
> · **INTEGER-OF-FORMATTED-DATE argument-2 IS a one-line row and is NOT blocked by PB12's per-position redesign** —
>   §15.6 Table 21 types BOTH its positions "Anum or Nat", exactly like SECONDS-FROM-FORMATTED-TIME which already
>   carries a single-kind string row. TEST-FORMATTED-DATETIME is the third function with that shape and the same
>   omission. Do not defer these three behind PB12.

### PB20 · [MAJOR] · references · ✅ LANDED 2026-08-02 (DEVLOG 1146) — the nonexistent clause is gone and GR6 has ONE implementation
> **21 occurrences across 14 files** (more than the batch reported), all swept to §8.4.3.3.4 GR6. The rule now
> lives once, on `RefModPlace.CategoryOf`, replacing THREE partial copies of which none was right — the base
> case (GR6 PRESERVES the category) is the arm all three lacked.
> ⚠ **LATENT BY DESIGN, and the tests say so:** Conformance stayed 4174/4174 because no rule the §15.3 screen
> consults today demands class boolean. The value is that **PB19's INTEGER-OF-BOOLEAN row is now unblocked**.
> ⚠ **§8.8.4.2.4 IS REAL** (Comparison of numeric operands, 15 occurrences) and sits one character from the
> fabricated clause — the guard regex carries a `(?<![.0-9])` boundary, and a naive grep-replace corrupts it.
> Guard: `RefModCategoryDriftTests` (13 cases; made to FAIL once before being trusted).
> The finding as originally recorded follows.

### PB20 (as found) · a NONEXISTENT clause "§8.4.2.4" is cited in ~9 files, and the rule it stands for is wrong
> ⛔ **`python scripts/spec/cite.py --check 8.4.2.4 …` → "there is no clause §8.4.2.4 in the transcription"**
> (verified independently, not taken from the agent). §8.4.2 has only .1/.2/.3; reference modification is
> §8.4.3.3. The citation is INHERITED across `ExpressionBinder`, `MoveBinder` (×3), `InitializeBinder`,
> `OoBinder` (×3), `Place`, `ReferenceResolver`, `MoveClassifier` and `IntrinsicArgumentRules` — CLAUDE.md rule 1's
> exact failure mode, at scale.
> **AND THE SUBSTANCE IS WRONG TOO, not just the number.** The code treats EVERY `RefModPlace` as class
> ALPHANUMERIC. The real rules (§8.4.3.3.4, all `--check`ed): **GR1** makes a leftmost-position boolean,
> alphanumeric or national "respectively" by identifier-1's class; **GR2** re-reads a usage-DISPLAY item of
> another category as alphanumeric; **GR3** re-reads a usage-NATIONAL item of another category as national;
> **GR6** gives the unique data item "the SAME class, category, and usage as that defined for identifier-1"
> except as listed. So a ref-modified **BOOLEAN** item stays BOOLEAN — and `ConditionRenderer` already knows this
> ("national/boolean ref-mod stays national/boolean"), so the codebase contradicts itself.
> ⚠ **THIS BLOCKS PB19's INTEGER-OF-BOOLEAN ROW:** a naive boolean screen over the current
> always-alphanumeric `ClassOfPlace` would falsely reject **the standard's own Annex D worked example**,
> `FUNCTION INTEGER-OF-BOOLEAN (bit-item (1:6))`. Fix the class rule FIRST, then add the row.

### PB21 · [MAJOR] · intrinsics · ⛔ OPEN — three `…Real` runtime members do not exist, so conforming source emits a raw Roslyn CS0117
> **The PB2/PB14 family again, in the integer date/boolean group.** `IntrinsicRenderer.AnyRealArgument` routes a
> floating-point argument to `CobolIntrinsics.<Name>Real`, and **`IntegerOfBooleanReal`, `IntegerOfDateReal` and
> `IntegerOfDayReal` exist nowhere in the runtime** — so
> `COMPUTE R = FUNCTION INTEGER-OF-DAY(1.995046E6)` binds clean and then fails Roslyn with **CS0117** at every
> edition. A float argument is LEGAL here (a COMP-2 item is class numeric).
> ⛔ **AND THE GUARD THAT SHOULD HAVE CAUGHT IT IS EXEMPT BY CONSTRUCTION:** `IntrinsicRealArgDriftTests` asserts
> the `…Real` counterpart exists for every exact method reachable with a real argument — **but exempts every
> integer-kind row**, which is precisely this group. Widening the guard is part of the fix, not a follow-up.

### PB22 · [MAJOR] · intrinsics · ⛔ OPEN — an unchecked `(long)(Int128)` cast makes every range guard unreachable past 2⁶³
> `IntrinsicRenderer.AsInt` emits `(long)(<Int128 expr>)` and `RoslynBackend` sets no `checkOverflow`, so the cast
> WRAPS. The correct §15.5.2 guard inside `CobolDate.IntegerOfDay` (1601..9999 / 1..366) is then unreachable for
> any argument ≥ 2⁶³: `FUNCTION INTEGER-OF-DAY(P * 100 + 62)` with `P PIC 9(18) VALUE 184467440737115466`
> (= 2⁶⁴ + 1995046) returns **143951** with NO EC-ARGUMENT-FUNCTION **even with checking ON**.
> ⚠ **ONE CAST FEEDS ELEVEN FUNCTIONS** — same unfixed root as the already-recorded DATE-OF-INTEGER row. Fix the
> cast, not the eleven call sites.

### PB23 · [MINOR] · date/time · ⛔ OPEN — a raw CLR exception where §15.3 requires EC-ARGUMENT-FUNCTION
> `INTEGER-OF-FORMATTED-DATE("YYYYWwwD", "9999W527")` passes every §15.3.1.7 subfield rule — so §15.48.3 r4 is MET
> and §15.48.4 r1 demands a returned value — and then `CobolDate.Analyze`'s `ISOWeek.ToDateTime` reconstructs past
> `DateTime.MaxValue` and throws **`System.ArgumentOutOfRangeException`**. §15.3 rule 14 (covering an incorrect
> value "for that argument OR FOR THE RETURNED VALUE") permits only EC-ARGUMENT-FUNCTION or the
> implementor-defined result, never a CLR fault.
> ⚠ **THE WINDOW IS TWO DAYS WIDE, NOT ONE — and this CORRECTS THE ALREADY-LANDED ROW `AR-15.79.3-5`**, which
> cited only day 7. Recomputing ordinal = 360 + d against the 365-day year 9999 makes day-of-week **6**
> (10000-01-01) fault as well as day 7 (10000-01-02).

### PB24 · [MAJOR] · intrinsics · ⛔ OPEN — FUNCTION LENGTH is wrong or absent on four shapes
> · **A VARIABLE-LENGTH GROUP folds to a WRONG COMPILE-TIME CONSTANT** — no arm recognises §8.5.1.12, so a
>   dynamic-length child contributes 0 and a dynamic-capacity table is counted as ONE occurrence. Silent.
> · **A REFERENCE-MODIFIED argument** — `FUNCTION LENGTH(WS-NAME(1:5))`, legal per §8.4.3.3.3 SR5 / §8.4.3.3.4 GR6
>   (both `--check`ed) — hits `BindLengthFold`'s `RefModPlace` arm, returns `BoundExprError`, and renders as
>   NotImplemented: compiles clean, throws at RUN TIME (the PB7/DA7 wrong-stage family).
> · **The PHYSICAL argument (§15.50.4 r8) does not exist at all** — no grammar production, no catalog arity slot.
> · **The §15.50.4 r9 rounding step is absent** — `BindLengthFold` emits a bare width with no rounding anywhere.

### PB25 · [MINOR] · intrinsics · ⛔ OPEN — LOWER-CASE/UPPER-CASE: the LOCALE arm is refused and a figurative argument aborts at run time
> · **`FUNCTION LOWER-CASE(x LOCALE loc)`** — the bracketed `[LOCALE locale-name-1]` arm of §15.57.2 is REFUSED
>   rather than diagnosed as the §A.4.9 non-support it is.
> · **A FIGURATIVE CONSTANT or ALL-literal argument compiles clean and ABORTS AT RUN TIME** —
>   `IntrinsicRenderer.StrArgVisitor`'s `Visit(BoundFigurative)` / `Visit(BoundAllLiteral)` both render
>   `EmitText.LoudValue`. Wrong stage again, and NOT a duplicate of the residue's "figurative as a §15 string
>   argument" row: this is the same defect reached through a different visitor.

### PB26 · [MAJOR] · exceptions · ⛔ OPEN — the ambient EC-ARGUMENT-FUNCTION gate is a HAND-MAINTAINED SWITCH
> A domain guard such as LOG10's raises only when `ExceptionState.ArgumentFunctionChecking` is set, and that
> ambient gate is emitted **only for the statement kinds enumerated in `EcBinder#DirectIntrinsic`'s hand-written
> `switch`** — so the identical function reference raises in one statement and is silent in another. That is a
> hand-maintained list where a STRUCTURE belongs (CLAUDE.md rule 5), and it is the general form of the
> receiver-shape defect PB13 hit: **an exception's reachability must not depend on which statement encloses it.**

### PB27 · [MINOR] · locale · ⛔ OPEN — the §A.4.9 non-support is loud everywhere EXCEPT the places that leak silently
> The locale module's documented non-support (`COBOLNET1518`, owner-ratified 2026-07-03) is correct and loud for
> the FUNCTION forms — 33 of this batch's 36 NOT-IMPLEMENTED rows are that one ratified disposition and are NOT
> defects. Three leaks are:
> · **The SPECIAL-NAMES `LOCALE` clause is SILENTLY ACCEPTED**, not a parse error as previously recorded: `LOCALE`
>   is not a lexer token, and `specialNameEntry`'s `implementorSwitchEntry` / `genericClause` catch-alls swallow
>   it. The one place in the disposition where the reject is not loud.
> · **`LOCALE-TIME`'s catalog row declares `ArgKinds "is"`** — argument-1 as integer — where §15.53.3 r1 and the
>   normative §15.6 Table 21 ("Anum1 or Nat1, Loc2") require alphanumeric/national of 6 character positions.
> · **`LOCALE-TIME-FROM-SECONDS` carries `IntroducedIn 2002`**; the function belongs to the 2014 date/time
>   package (argument-1 is in standard numeric time form, §15.5.5 — a form no 2002 function produced).


### ⛔ BATCH §15.32–15.44 OPENED PB10–PB16 (2026-07-30) — 82 findings, SEVEN root causes

The batch adjudicated 67 rules over 13 functions and returned **31 DIVERGES · 22 PARTIAL · 14 NOT-IMPLEMENTED ·
ZERO CONFORMS**. Every subject was refuted by an independent agent and every refutation was a downgrade or a
widening. **The 82 findings are not 82 defects** — clustering them by root cause gives the seven items below plus
a residue. ⚠ Do NOT work these finding-by-finding: most are one cause seen from ten functions, and fixing the
cause retires the whole column. Repros: `docs/rearchitecture/evidence/PHASE-B-15.32-15.44-findings.md` (F-numbers
below index it).

**⛔ SEVERITY ORDER, and PB10 is the CLAUDE.md rule 4 red line.**

### PB17 · [MAJOR] · references · ⛔ OPEN — a function-identifier as a SUBSCRIPT or a REF-MOD position compiles clean and throws
> **SPLIT OUT OF PB10, because it is a different root cause and would have been hidden inside a grammar fix.**
> The PB10 findings said these positions were "rejected". They are not — they PARSE:
> ```
> MOVE E(FUNCTION INTEGER(3)) TO T      -> compiles clean; at RUN TIME:
>     NotImplementedCobolFeatureException: reference 'E(FUNCTION INTEGER(3))'
> MOVE A(FUNCTION INTEGER(3):2) TO T    -> same
> ```
> That is the PB7/DA7 WRONG-STAGE family. Root cause: both positions lex in SUBSCRIPT mode, so the operand
> reaches `ReferenceResolver`'s flat-token segment renderer (`RenderSegment`), which has arms for literals,
> data-names, the operators and parentheses — and NONE for a nested FUNCTION call, so the reference fails to
> resolve and falls through to the loud runtime guard. §8.4.2.3.3 admits an arithmetic expression as a subscript
> and §8.4.3.3.3 SR4 admits one as a ref-mod position, and §8.4.3.1.2 Format 1 makes a function-identifier an
> identifier — so both are legal source. ⚠ Fixing this in the GRAMMAR is the wrong move: the D10/PHASE-15 plan
> removes SUBSCRIPT mode entirely, so the durable fix is the segment renderer, not a new alternative.

### PB10 · [MAJOR] · references · ✅ LANDED (DEVLOG 1136 + 1143) — a function-identifier in the identifier-N sending positions

> **✅ THE INSPECT HALF CLOSED 2026-08-02 (DEVLOG 1143) — the position the grammar could not decide alone.**
> identifier-1 is SENDING only in Format 1, and the split was DERIVED, not taken from this entry's own summary:
> §14.9.22.4 GR1 concedes only that "for purposes of determining its length, identifier-1 is treated as a sending
> data item" — a SCOPED concession that would be unnecessary if it were generally sending; GR7 has each match
> "tallied (format 1) or replaced by literal-3 (format 2)"; and GR20 makes a Format 4 execute AS a Format 2 over
> the same identifier-1. So Formats 2/3/4 RECEIVE and §8.4.3.2.3 SR1 bars a function-identifier there
> (all `--check`ed). Grammar admits ADDITIVELY, binder screens per format — **`COBOLNET1632`
> (`function-identifier-receiving`), named after the RULE and not the statement**, because PB10's remaining
> positions and PB17 want the same verdict and a statement-shaped code invites a second one for the same sentence.
>
> **⭐ THE SCREEN KEYS ON THE PHRASES PRESENT, NOT ON A FORMAT NUMBER — and that is the whole subtlety.** A screen
> keyed on "TALLYING present ⇒ Format 1" ACCEPTS Format 3 (`TALLYING … REPLACING …`), which is illegal: TALLYING
> genuinely is present, and the REPLACING phrase still modifies identifier-1. Keying on
> REPLACING-or-CONVERTING is also **exactly the predicate the emitter already computed as `mutated`** — one fact
> with one representation, so the bind-time screen and the store-back decision cannot drift apart. The emitter
> now ASSERTS that correspondence (a non-field target reaching the store is loud, not a silently dropped write).
> Pinned by three negative fixtures, one per format branch: `pb10-inspect-fn-replacing` · `-converting` ·
> **`-tallying-replacing`** (the one a naive screen passes).
>
> **STRUCTURAL:** `BoundInspect.Target` moved `Place` → `BoundOperand`, since a function result is a VALUE with
> no place. Two analysis sites moved with it and both were semantic, not mechanical: `BoundStores` now asks
> `(Target as BoundFieldOperand)?.Place` (a function target can never be a store — the same rule stated twice,
> since the guard already requires REPLACING/CONVERTING), and `UsageCollectionPass` walks it as an operand.
> ⚠ **THE LEGACY BINDER WAS CARRIED, NOT ABANDONED** (this entry's own third caution): `inspectStatement` is in
> the SHARED `Core/CobolIO.g4`, so `.dataReference()` became nullable and `StringStatementBinder.BindInspect`
> would have dereferenced it. It now declines the new shape cleanly, which is the established
> unsupported-statement contract rather than a crash.
> **18 findings over 10 subjects — legal COBOL rejected, the widest defect this review has surfaced.**
> §8.4.3.1.2 Format 1 makes a function-identifier an IDENTIFIER, and §8.4.3.2.3 SR1 bars it only from a RECEIVING
> operand — so EVERY identifier-N SENDING position admits one. The grammar reaches `functionCall` from just FOUR
> places (`primaryExpression`, `displayStatement`, `moveSendingOperand`, `strUnstrSender`), so the rest reject it:
> `WRITE`/`REWRITE`/`RELEASE … FROM`, `INSPECT` identifier-1, `INITIALIZE … REPLACING … BY`, a SUBSCRIPT, and a
> REFERENCE-MODIFIER's positions. `CobolIO.g4`'s own DA4 comment states the principle and then applied it to
> STRING/UNSTRING only. ⛔ **Fix the DISPATCH, not the ten call sites** — this is one rule written in four places
> and wanted in a dozen. Interacts with PB8: ref-mod of a function result now works, so the ref-mod POSITIONS
> should too.
>
> **⛔ THREE MEASURED CORRECTIONS TO THE FINDINGS ABOVE — made by running them, before any code was written.**
> · **The SUBSCRIPT and REFERENCE-MODIFIER positions are NOT parse errors.** The finding said "rejected"; they
>   COMPILE CLEAN and throw `NotImplementedCobolFeatureException` at RUN TIME — `MOVE E(FUNCTION INTEGER(3))`
>   reaches the runtime as `reference 'E(FUNCTION INTEGER(3))'`. That is the PB7/DA7 wrong-stage family and a
>   DIFFERENT root cause from the parse rejections: the SUBSCRIPT-mode capture reaches `ReferenceResolver`'s
>   segment renderer, which has no arm for a nested FUNCTION call. Do not fold it into the grammar fix.
> · **INSPECT is FORMAT-DEPENDENT and the finding's blanket SR1 citation over-claims.** §14.9.22.2 has four
>   formats; identifier-1 is a SENDING operand only in Format 1 (TALLYING). In Formats 2/3/4
>   (REPLACING / TALLYING-and-REPLACING / CONVERTING) it is MODIFIED IN PLACE, so §8.4.3.2.3 SR1 ("a
>   function-identifier shall not be specified as a receiving operand", validated) BARS one there. Admitting it
>   unconditionally would accept illegal source. Grammar admits, binder screens per format.
> · **⚠ THE FIX TOUCHES THE LEGACY COMPILER, which is why it is not a one-file change.** `writeFrom`,
>   `rewriteFrom`, `releaseFrom` and `initializeReplacingItem` live in the SHARED `CobolParserCore.g4`/`Core`
>   grammar, so routing them through `moveSendingOperand` breaks ~8 call sites across BOTH `Cobol.Net.Compiler`
>   AND `CobolSharp.Compiler` (`FileIoBinder`, `DataStatementBinder`, `InitializeBinder`,
>   `VersionConformancePass`), each of which reads `.dataReference()`/`.literal()` off the context directly.
>   Attempted and REVERTED rather than landed half-done. The legacy engine survives until the P15 cut-over, so
>   its binders must be carried, not abandoned — budget for two compilers.
>
> **✅ LANDED — the four positions the STANDARD ITSELF defines as MOVE sending items**, all verified through
> their own binder paths (SequentialIo · KeyedIo · SortBinder · InitializeBinder), not inferred from the shared
> helper: `WRITE … FROM` · `REWRITE … FROM` · `RELEASE … FROM` · `INITIALIZE … REPLACING … BY`. All four bind
> through the ONE `SequentialIoBinder.WriteSource` helper, so the next FROM phrase inherits the arm.
> **GOLDEN** `conformance:2023/pb10_function_identifier_sending`.
> ⛔ **THE GRAMMAR CHANGE IS ADDITIVE — `(functionCall | dataReference | literal)`, NOT a rewrite to
> `moveSendingOperand` — and the reason is recorded in the `.g4`:** collapsing to the shared rule DELETES the
> generated `.dataReference()`/`.literal()` accessors and breaks ~8 call sites across BOTH compilers, because
> this grammar is shared with the legacy `CobolSharp.Compiler`, which survives to the P15 cut-over. Tried,
> reverted, then done additively. The unification belongs to P15, when the legacy side is DELETED rather than
> migrated — doing it now is work that gets thrown away.
>
> **⛔ STILL OPEN ON THIS ITEM: INSPECT, and it needs a per-FORMAT screen rather than a grammar widening.**
> §14.9.22.2 has four formats; identifier-1 is a SENDING operand only in Format 1 (TALLYING). In Formats 2/3/4
> (REPLACING / TALLYING-and-REPLACING / CONVERTING) it is modified IN PLACE, so §8.4.3.3.3… no — §8.4.3.2.3 SR1
> ("a function-identifier shall not be specified as a receiving operand", validated) BARS one there. Admitting it
> unconditionally would ACCEPT ILLEGAL SOURCE, so the grammar admits and the binder screens by format.
>
> **THE SPEC IS ALREADY DERIVED for the remaining half**, so the next session starts at code, not at the standard:
> §14.9.51.4 GR5a makes WRITE FROM equivalent to `MOVE identifier-1 TO record-name-1` (validated), and
> §14.9.20.3 SR4 says INITIALIZE REPLACING's identifier-2 is "the SENDING item" of a MOVE (validated) — so both
> admit exactly what `moveSendingOperand` admits, and using that ONE rule is the standard's own definition of
> the operand rather than a convenience.

### PB11 · [MAJOR] · date/time · ✅ LANDED (DEVLOG 1139 + 1144) — the §15.3 format GRAMMAR *and* the value rules
> **✅ THE RECOGNIZER EXISTS: `DateTimeFormatGrammar`, and rule 2 of all SEVEN format-taking functions is
> enforced** (`COBOLNET1631`). The legal set is CLOSED — §15.3.1.1's six date formats, §15.3.2's twelve time
> formats (four common-time shapes × local/UTC/offset), and §15.3.4's combined = date + `T` + time with **basic
> paired to basic and extended to extended** — so membership is the test. §15.39.3 r1 makes argument-1 a
> LITERAL, so it is decided at BIND time. Goldens: `conformance:2023/pb11_datetime_format_grammar` (all six date
> formats + the time and combined variants) · `negative/pb11-format-wrong-kind` · `negative/pb11-format-kind-chimera`.
> ⚠ **THE KIND IS A SET PER FUNCTION, NOT ONE VALUE — the corpus caught the first version.** Four of the seven
> rules name more than one kind (§15.48.3 r2 date-or-combined · §15.79.3 r2 time-or-combined · §15.92.3 r2
> date-or-time-or-combined), and inferring the siblings by NAME ANALOGY rejected the legal corpus program
> `2014/formatted_datetime`. Each row now quotes its own rule.
> ✅ **PB16 IS RETIRED BY CONSTRUCTION**: §15.3.3.2 makes the seconds-fraction maximum implementor-defined
> (≥ 9), and it is now DOCUMENTED at 18 (`CONFORMANCE.md` item 87) — exactly where `long` stops being exact — so
> the ≥19-digit overflow is unreachable rather than separately guarded.
>
> **✅ THE VALUE RULES CLOSED 2026-08-02 (DEVLOG 1144), and the recogniser needed only to RETURN WHAT IT ALREADY
> KNEW.** `IsTimeOfWidth` had always decided local-vs-UTC-vs-offset in order to answer "is this a time format" at
> all, then collapsed the three into a bool. `Describe` now returns `DateTimeFormatInfo(Kind, Zone)`; the
> alternative — re-inspecting the format string at the call site — would have been the same rule written twice.
> ⚠ **THE PREVIOUS SUMMARY OF THESE RULES WAS WRONG IN TWO PLACES, both found by reading them rather than the
> summary.** It said "§15.40.3 r4/r5 (argument-3 in [0,86400), |argument-4| ≤ 1439)". In fact **r4 says
> "argument-3 shall be a value in STANDARD NUMERIC TIME FORM"** — a defined term, not an inline range — and that
> range is fixed by the **§7.3.17 LEAP-SECOND directive**, not by the number of seconds in a day: OFF gives
> `< 86,400` and **ON gives `< 86,401`**. We support only the default OFF (already documented), so the constant
> is cited to the directive rather than written as a bare 86400, which is what makes the ON case a documented
> gap instead of a silent wrong answer.
> · **§15.40.3 r6 / §15.41.3 r5 — BIND time** (`COBOLNET1633`): the offset argument is barred when the format's
>   time portion is LOCAL. Decidable at compile time because rule 1 makes argument-1 a LITERAL and the argument's
>   presence is syntactic. Previously the argument bound cleanly and was then SILENTLY DISCARDED.
>   ⚠ **ONE-SIDED DELIBERATELY:** the converse is explicitly LEGAL — omitting it for a UTC/offset format "shall
>   be evaluated as though 0 were specified" (§15.40.3 r7 / §15.41.3 r6) — so screening both ways would reject
>   conforming source. The positive golden pins the omitted form for exactly that reason.
> · **§15.40.3 r5 / §15.41.3 r4 — RUN time**: `|offset| ≤ 1439`, enforced nowhere before. The boundary renders as
>   `+2359`, which is the spec NOTE's own explanation of the number (23 h 59 m, one minute less than a day).
> · **§15.41.3 r3 / §15.40.3 r4 — RUN time**: the seconds argument in standard numeric time form. A value of
>   100000 (≈27.7 h) used to FABRICATE `hh=27` with no exception condition; it now takes the §15.3 EC default.
>   The bound is scaled UP into `Int128` rather than scaling the value down, so a fractional overshoot cannot be
>   truncated into range.
> **GOLDENS:** `pb11_datetime_format_grammar` extended (omitted-offset for both functions, ±1439 boundaries,
> the 86399 boundary and the over-range EC default) · `negative/pb11-offset-arg-local-format-time` ·
> `negative/pb11-offset-arg-local-format-datetime`.
> ⚠ **RESIDUE, NAMED:** §15.3.1.3/.5/.7's permitted-value ranges constrain the DATA in a formatted string on the
> PARSING side (INTEGER-OF-FORMATTED-DATE / TEST-FORMATTED-DATETIME / SECONDS-FROM-FORMATTED-TIME), which is a
> different seam from the FORMATTING side closed here — `CobolDate.Analyze`'s per-field bounds already cover part
> of it. Not folded in on a guess; it needs its own reading of those three clauses against `Analyze`.

### PB11 (the original entry) · the §15.39/40/41 date-time FORMAT rules are validated nowhere
> **19 findings over the 4 FORMATTED-* functions.** §15.39.3 r2, §15.40.3 r2–r7 and §15.41.3 r2–r6 are enforced
> nowhere: a TIME format passes where a DATE format is required, a combined/basic/extended chimera is accepted, an
> out-of-range argument-2/3/4 is never checked, and argument-4 is silently DISCARDED when the format admits no
> offset. The failure mode is a FABRICATED value with no exception condition, not mere over-acceptance.
> ⛔ **The fix is a RECOGNIZER, not added checks**: `CobolDate.Tokenize` enforces a per-character class and a
> per-field width, and never answers "is this string one of the formats §15.5 defines".

### PB12 · [MAJOR] · intrinsics · ◑ HALF LANDED (DEVLOG 1138) — the §15.3 argument-class screen for this batch's functions
> **✅ SEVEN ROWS ADDED**, each read from its own §15.x.3 argument rule and mechanically cited: EXP · EXP10 ·
> FRACTION-PART · INTEGER (`'n'`, "shall be of class numeric") · FACTORIAL (`'i'`) · EXCEPTION-STATEMENT ·
> EXCEPTION-STATUS (`' '`, no arguments). Negative fixture `pb12-exp-alphanumeric-argument`.
> ⚠ FACTORIAL is PARTIAL by construction: §15.36.3 r1 is "an integer GREATER THAN OR EQUAL TO ZERO" and the
> screen sees only the class half; the value half is EC-ARGUMENT-FUNCTION's at run time.
>
> **⛔ SIX FUNCTIONS DELIBERATELY GOT NO ROW, and the omissions are the remaining work:**
> · **FIND-STRING + the four FORMATTED-* functions** take MIXED argument classes (an alphanumeric/national
>   format string plus integer operands) and this table carries ONE kind per FUNCTION. A row would screen every
>   position by the first one's class and REJECT LEGAL SOURCE. Needs a per-POSITION schema — a design change,
>   which is the half of PB12 still open.
> · **HIGHEST-ALGEBRAIC** — §15.43.3 r1 admits "a data item of category numeric OR NUMERIC-EDITED", and
>   §8.5.2.1 Table 2 files numeric-edited under class ALPHANUMERIC, so an `'n'` row would reject an argument the
>   standard ADMITS. Left unscreened until a kind can express "category numeric or numeric-edited".
>
> **⚠ A WIDENING OF `'n'` WAS ATTEMPTED, REFUTED AND REVERTED — recorded so it is not re-argued a third time.**
> Seeing `FUNCTION EXP(<numeric-edited>)` rejected, the argument runs: §15.3 type 10 admits "an ARITHMETIC
> EXPRESSION or a numeric data item", verbatim what type 6 admits for `'i'`, so numeric-edited should de-edit
> and be admissible. It does NOT follow — §8.8.1.1 defines what an arithmetic expression may BE: "an identifier
> referencing a NUMERIC DATA ITEM, a numeric literal, the figurative constant ZERO …" (validated). A
> numeric-edited item is not a numeric data item, so it is neither of type 10's alternatives. **The existing
> negative fixture `pb1-numeric-arg-numeric-edited` caught the widening**, which would have silently ACCEPTED
> ILLEGAL COBOL. The refutation now sits beside the screen in `IntrinsicArgumentRules`.
>
> **✅ AND THE `'i'` ARM HAS NOW BEEN BROUGHT INTO LINE — OWNER DECISION 2026-08-02 (DEVLOG 1142).** The two arms
> had come to rest on readings of §8.8.1.1 that could not both be right: `'n'` refuted the de-editing reading
> while `'i'` was landed ON it, citing DA6's screen, a corpus golden and an AssertSpec test. Re-derived, every
> citation `--check`ed, the exclusion holds for BOTH — and the decisive evidence was three lines above this
> paragraph the whole time:
> **⭐ WHEN THE STANDARD MEANS TO ADMIT A NUMERIC-EDITED ARGUMENT, IT SAYS SO EXPLICITLY.** §15.43.3 r1
> (HIGHEST-ALGEBRAIC) admits "a data item of category numeric **or numeric-edited**". §15.3's type 6 and type 10
> say "an arithmetic expression … or an integer/numeric data item" and name numeric-edited NOWHERE. A reading
> that makes numeric-edited implicit in "numeric data item" makes §15.43.3 r1's second alternative redundant.
> Supporting: §8.5.2.13 calls it a "numeric-edited data item" (a distinct defined term); Table 2 files it under
> class ALPHANUMERIC/NATIONAL; and de-editing is GRANTED by the MOVE rules (§14.9.25.4 GR5/GR6d1) and nowhere
> extended to arithmetic — a grant that would be unnecessary if it were general.
> **Both external oracles were consulted, per the owner's standing rule for interpretation questions:** no NIST
> program depends on it (the entire NIST corpus stayed green across the flip — the ONLY three reds were the
> three artifacts that encoded the old premise), and GnuCOBOL's suite exercises de-editing exclusively under
> MOVE, every case titled "MOVE with de-editting to …".
> ⚠ **THE LESSON THAT SURVIVES IS ABOUT EVIDENCE, NOT ABOUT EDITING:** a corpus golden and a spec-derived unit
> test AGREEING is not independent corroboration when both were written from the same premise. Three artifacts
> agreed for months and all three were wrong together. New fixture: `pb1-integer-arg-numeric-edited`.

### PB12 (the original entry) · the §15.3 argument-class screen is absent for most of these functions
> **13 findings over 10 subjects.** PB1's `IntrinsicArgumentRules.Verified` screen only runs for functions with a
> row, and these have none — so their argument rules are unenforced and illegal source computes a value. This is
> PB1's DESIGNED residue (the table grows as the review adjudicates each clause), so it is now due for exactly the
> clauses this batch adjudicated. ⚠ Two findings say the one-row fix is NOT sufficient: the class lattice cannot
> express two of §8.5.2.1 Table 2's classes (INDEX stays unscreened), and the screen is STRUCTURALLY UNREACHABLE
> for every phrase-keyword intrinsic (FIND-STRING, SUBSTITUTE, TRIM, CONVERT) — no `Verified` row can fix those.

### PB13 · [BLOCKER] · numerics · ✅ LANDED 2026-08-02 (DEVLOG 1141) — the cap is on the WORKING SCALE, and the fix is entirely emitter-side

> **THE ROOT CAUSE, stated as a mechanism rather than a symptom.** `CobolIntrinsics.FromDouble` lands a double at
> a WORKING scale and saturates at `Int128.MaxValue`. The store then rescales working→receiver scale, which
> **DIVIDES the saturation sentinel back down** — so the receiver's digit-capacity check, the one mechanism that
> would raise the size error, never sees it. *That* is why the saturation was silent rather than loud, and it is
> the whole defect. `CobolFloat.ToScaled` was never affected because it lands AT the receiver's scale, so its
> sentinel survives into the capacity check; only the working-scale path lost the evidence.
>
> **THE FIX — `ReceiverContext.FloatWorkingScale`, one rule, two consumers.**
> `ws = min(max(receiverScale, 9), 38 − receiverIntegerDigits)`. `ReceiverContext` gained `IntegerDigits`
> (measured from §13.18.40.3 SR14 `DigitPositions`, never the '9' count — under-counting is the one unsafe
> direction) and `Receiverless`. It restores both halves of the contract:
> · a value that FITS the receiver can no longer saturate (intDigits + ws ≤ 38);
> · a value that does NOT fit saturates to a sentinel that STILL exceeds the receiver after the rescale, so the
>   store RAISES the size error (§14.7.5 case 5 — `--check`ed). `COMPUTE R = FUNCTION EXP(700)` into `PIC 9(31)`
>   now reports SIZE ERROR where it stored a saturated value silently.
>
> **THE RECEIVER-LESS HALF — and the entry's recipe was wrong about it.** With no receiver there is no scale to
> quantize TO, so the `ws = 9` stand-in was arbitrary; §15.4.1 leaves the returned value's representation to the
> implementor under native arithmetic, and COBOL.NET's determination is that the float family's value **IS a
> binary64**. `ReceiverContext.None` renders now stay `Real`. Every consumer already had a `Real` arm and is more
> correct on it — a relation compares natively (§8.8.4.2.4), the text channel renders through the one
> `CobolFloat.Display` a float ITEM uses, and a MOVE source lands via `CobolFloat.ToScaled` at the receiver's
> scale. **`docs/CONFORMANCE.md` had already documented exactly this** ("a FLOAT-valued function renders through
> the same shortest-round-trip `CobolFloat.Display` a COMP-2 item does"); the code did not match its own
> determination. No runtime change was needed, and none was made.
>
> **THE SIBLING SWEEP (rule 4) FOUND THREE MORE SITES, AND THE ENTRY NAMED NONE OF THEM.**
> · **`**`** — `NumericRenderer.Power` reaches the SAME quantizer with the SAME formula, so
>   `COMPUTE R = 10 ** 30` into `PIC 9(31)` stored the identical wrong constant and `IF 10 ** 30 = 10 ** 31`
>   was TRUE.
> · **The NUMVAL family (§15.67/§15.68/§15.69)** — and this one is **PB5's OWN defect, in the sibling PB5 never
>   swept.** `Numval`/`NumvalC`/`NumvalF` still returned `long` and clamped at `long.MaxValue`, the very 9.2×10¹⁸
>   clamp PB5 widened to `Int128` in `FromDouble`. With the family's ≥6/≥9 working floor the clamp fires on
>   ORDINARY arguments: **`FUNCTION NUMVAL-F("1E+20")` returned 9223372036** — ten orders of magnitude out, with
>   no size error — where §15.69.4 r2 (`--check`ed) requires "an approximation of the numeric value represented
>   by argument-1". All three now return `Int128` and saturate through one shared `Rescaled` helper that also
>   bounds the exponent, because `Pow10.AsWide` WRAPS past 10³⁸ and a large `E±nn` would otherwise multiply by a
>   wrapped power — a plausible wrong value rather than a saturated one.
> · **`NumericRenderer.Align`** — no `Real` arm, so a receiver-less binary64 reaching a subscript / SET amount /
>   PERFORM VARYING / report VARYING / RETRY count was handed to a caller expecting a scaled integral. Already
>   reachable through a COMP-2 operand; the arm landed at the ONE choke point, not at forty call sites.
> ⚠ **THE GUARD FOUND THE NUMVAL FAMILY, NOT A HUMAN.** `FloatQuantizeHeadroomDriftTests` failed on its FIRST
> run against a site the sweep had missed by eye. Its first version matched only the float floor `, 9)` and
> passed over the three floor-`6` NUMVAL sites — the alternation is on `\d+` now. **The floor is part of the
> pattern, not part of the site**, which is the generalisable lesson: `ReceiverContext.WorkingScale(floor)` is
> one rule parameterised by the family's documented floor, so a family cannot get a cap the others lack — which
> is precisely the state PB5 left behind.
>
> **BLAST RADIUS, MEASURED not asserted.** The cap binds only past 29 integer digits, so no ordinary picture
> moved: the full Conformance corpus changed exactly ONE golden, `da2_function_as_text`, whose
> `Q1=[2.000000000]` became `Q1=[2]` — *toward* the "significant digits, no zero padding" determination its own
> header states and against which `2.000000000` was already wrong.
>
> **GOLDENS + GUARD:** `pb13_float_quantize_headroom` (promoted from `pending`; the two cases below) ·
> `pb13_float_quantize_siblings` (the four the sweep found) · `FloatQuantizeHeadroomDriftTests`, which proves both
> invariants over EVERY legal (integer-digits, scale) pair, pins the below-29 behaviour-neutrality, and fails on a
> hand-rolled `Math.Max(rcv.Scale, 9)` at either site — a mistake no runtime test can see, because it is correct
> for every ordinary picture, which is exactly how PB13 survived PB5. Design: `COBOLNET_NUMERIC_DESIGN.md` D18.
>
> **A SEPARATE FINDING THIS EXPOSED, not fixed here (see the residue below):** `10 ** 30` now returns
> 1000000000000000071935427891953 — the `Math.Pow` double approximation — where Int128 could hold 10³⁰ exactly.
> §8.8.1.2 r6 imposes no exactness requirement and §8.8.1.3 makes native implementor-defined, so it CONFORMS, but
> it contradicts our own documented native technique (design D3: "the exact Int128 fixed-point engine"). An
> integer exponent with fixed-point operands should evaluate exactly.
>
> The finding as originally recorded follows, for provenance.

### PB13 (as found) · [BLOCKER] · numerics — the float→fixed quantizer saturates SILENTLY, and PB5 closed on a false premise
> **7 findings over 4 subjects. HAND-REPRODUCED — both cases below were run, and the cluster summary that used to
> sit here UNDERSOLD it.** It said "reachable from a declarable receiver"; the sharper case needs no receiver at
> all. A pending golden pins the correct behaviour: `conformance:2023/pb13_float_quantize_headroom` (registered
> under `pending`, and verified to FAIL on both cases today).
> ```
> 01 R PIC 9(31).
>     COMPUTE R = FUNCTION EXP(70) ON SIZE ERROR … NOT ON SIZE ERROR …
>   -> NO-SIZE-ERROR ;  R = 0170141183460469231731687303715
>      §15.34.4 r1 + §15.4.1 require ≈2.5154386709191670×10³⁰ — WRONG BY A FACTOR OF ~15, SILENTLY.
>
> IF FUNCTION EXP10(30) = FUNCTION EXP10(31)   ->  TRUE
>   -> two values a FACTOR OF TEN apart compare EQUAL, with NO receiver in the statement.
> ```
> **⛔ PB5's CLOSING PREMISE IS FALSE TWICE, and that is why this survived.** `FromDouble`'s doc asserted the
> clamp was "past the 10¹⁸ any PICTURE can describe, so unreachable from a declarable receiver". (1) A PICTURE
> reaches 31 digit positions (CA33 caps it there), so `PIC 9(31)` holds 10³⁰. (2) The clamp fires at the
> FUNCTION's quantization point, BEFORE any store — a relation operand renders under `ReceiverContext.None`
> (scale 0 ⇒ ws = 9), so both sides saturate identically. The comment is now corrected in place.
> **⛔ DO NOT WIDEN THE CLAMP CONSTANT.** At `ws = 9` the intermediate needs (receiver integer digits + 9)
> decimal digits and `Int128` supplies ~38, so a 31-digit receiver is two digits short whatever constant is
> chosen. The fix is TWO-SIDED and the second half is the structural blocker:
> · **emitter** — `IntrinsicRenderer#RenderFloat` must choose the working scale against the receiver's
>   CAPACITY, not just its scale. ⚠ `ReceiverContext(int Scale, bool Real, CobolRounding, bool InSizeError)`
>   carries NO digit count, so threading capacity through it is the actual work;
> · **runtime** — the receiver-less case cannot be fixed by any emitter-side choice, so `FromDouble` must stop
>   SILENTLY saturating. §14.7.4 makes an intermediate overflow a size-error condition; loud is the minimum.
> ⚠ **NOT reproduced: the "INTEGER's value depends on its receiver" half.** `FUNCTION INTEGER(-1.5)` returns −2
> into scale-0, scale-4 and scale-9 receivers alike, and `COS(1)` into a 9- vs 17-digit receiver differs only by
> ordinary receiver rounding of one value, which no rule forbids. Finding F80's INTEGER claim is about the
> saturation path (same root cause as above), not about receiver-dependence — treat the §0 COS remark as
> unproven until someone produces a case where the VALUE, not its stored rounding, changes.

### PB14 · [MAJOR] · numerics · ⛔ OPEN — STANDARD / STANDARD-DECIMAL arithmetic + an intrinsic argument emits raw CS1503
> **4 findings over 4 subjects.** An arithmetic-expression argument under `ARITHMETIC IS STANDARD` reaches the
> backend as a raw Roslyn `CS1503`, or silently computes the wrong value. This is **the PB2 shape on the Dec axis
> instead of the Real axis** — PB2 fixed one arm of the same dispatch. One of the four is a missing runtime member
> (`CobolIntrinsics.FactorialReal` does not exist yet `RenderNum` routes to it).

### PB15 · [MAJOR] · intrinsics · ⛔ OPEN — the §15.x RESULT-TYPE tables are ignored for the FORMATTED-* family and TRIM
> **4 findings.** §15.39.1/§15.40.1/§15.41.1 make the function's type follow ARGUMENT-1 (national argument ⇒
> national function); `IntrinsicCatalog` hardcodes `Alphanumeric`. Wrong result category propagates into Table-16
> MOVE legality and the string channels, so it under-rejects as well as mis-typing.

### PB16 · [MINOR] · date/time · ✅ RETIRED BY PB11 — a fractional-seconds field of ≥19 's' characters overflowed
> `EmitFormatted` computes `(long)Pow10.AsWide(s.Width)`, which overflows past 10¹⁸. Narrow, contained, and a
> crash rather than a wrong value.

### PB18 · [MINOR] · numerics · ⛔ OPEN — a native `**` with an INTEGER exponent goes through `Math.Pow`, losing exactness Int128 could hold

> **Found by PB13's sibling sweep, and only visible once PB13 stopped the saturation from masking it.**
> `COMPUTE R = 10 ** 30` into `PIC 9(31)` returns **1000000000000000071935427891953** — the binary64
> approximation — where the exact 10³⁰ fits Int128 (31 digits of 38) comfortably.
> **It CONFORMS**: §8.8.1.2 r6 places no exactness requirement on exponentiation, and §8.8.1.3 makes native
> arithmetic implementor-defined end to end. **But it contradicts our own §8.8.1.3 implementor documentation** —
> numeric design D3 declares the documented native technique to be "the exact Int128 fixed-point engine", and
> `NumericRenderer.Power` routes every fixed-point base to `System.Math.Pow` regardless of the exponent.
> The standard-decimal path already does this correctly (`CobolDec.Pow` — binary square-and-multiply over exact
> SDIDI multiplication, §8.8.1.5.4 r2a–r2e), so the shape to copy exists: an INTEGER exponent with fixed-point
> operands should evaluate by repeated exact `Int128` multiplication, with the existing size-error escape at the
> Int128 boundary, and only a NON-integer exponent should fall through to the double approximation.
> ⚠ Scope note: this is the same "an implementor-defined approximation used where an exact result was available"
> family as PB13, not a saturation bug — no value is silently wrong, only less exact than documented.

### ⚠ RESIDUE — 16 findings not yet clustered, each its own root cause
> Individually smaller but several are "rejects legal COBOL": an alphanumeric/national CONSTANT-NAME refused in
> every intrinsic argument position (§13.10.4 GR1) · no COBOL word may contain an UNDERSCORE though the standard
> has permitted it since COBOL-2002 · the hexadecimal-national literal `NX"…"` (§8.3.3.5.2 Format 2) has NO lexer
> rule and degrades SILENTLY into two arguments · `EXCEPTION-STATEMENT` returns `GO` where Table 12 requires
> `GO TO` · `EXCEPTION-STATUS` silently truncates an exception-name at 31 characters · `EcBinder.EcWrap` collapses
> per-CONDITION `WITH LOCATION` into ONE per-STATEMENT bool, so one stray directive contaminates every other
> condition's §15.32.3 r1 answer · `GOBACK … RAISING` carries no location at all (`BoundRaising` has no such
> field, unlike its sibling `BoundRaise`) · `FIND-STRING` truncates argument-3 from long to int · a FIGURATIVE
> CONSTANT as a §15 string argument compiles and throws at run time · `HIGHEST-ALGEBRAIC` rejects every
> floating-point argument and mis-folds unsigned COMP-5 at the 19–31-digit tier. See the evidence ledger.



### PB1 · [MAJOR] · intrinsics · ✅ LANDED — the CLASS half (DEVLOG 1117); a named residue stays open

> **`COBOLNET1627` (`intrinsic-argument-class`), edition-invariant, strict-reject with a `--permissive` warning —
> the DA6 disposition for the sibling §8.8.1.1 question.** Five of the twelve rows CLOSE (ABS, PRESENT-VALUE,
> RANDOM, RANGE, REM — bare class constraints, now fully enforced); five go **PARTIAL** because their rule has a
> second half this screen does not reach, and one is unaffected. Half a rule enforced is PARTIAL, not CONFORMS.
>
> **⛔ THE RECIPE BELOW WAS WRONG, AND THE CORRECTION IS THE VALUABLE PART.** "Consume `sig.ArgKind(i)`" is what
> this entry prescribed. Doing exactly that made the comprehensive gate return **12 failing corpus programs, every
> one of them legal COBOL** — `FUNCTION BYTE-LENGTH(<numeric>)` rejected, though §15.14.3 admits an argument of
> ANY class, and the nine rows with an empty `ArgKinds` default to `'n'`, which would have screened `LENGTH` as
> numeric-only. **The table is not merely UNREAD, it is UNVERIFIED — and it was unverified *because* it was
> unread.** Those codes were dispatch hints that drifted freely for years precisely because nothing consulted
> them. Wiring 79 unaudited declarations into a rejection path converts one silent defect into 79 chances to
> reject valid source.
> So the screen is driven by `IntrinsicArgumentRules.Verified` — a table whose every entry carries **the ISO
> clause it was read from**. Eleven entries (§15.7, §15.70–15.79: exactly the functions Phase B adjudicated). A
> function absent from it is screened exactly as before, so landing this could not regress anything, and the table
> grows as the review adjudicates each clause. Asserting the other 68 rules from an unaudited hint column would be
> fabrication, not completeness.
>
> **RESIDUE, each recorded on its own inventory row as PARTIAL — not lost, and not silently deferred:**
> · a LENGTH half — `AR-15.78.3-1` REVERSE ("at least one character position") and `AR-15.70.3-1` ORD ("one
>   character position in length");
> · a STRONGLY-TYPED GROUP exclusion — `AR-15.71.3-1` / `AR-15.72.3-1`, which the binder has no strong-type
>   predicate to screen at this seam;
> · a CROSS-ARGUMENT rule — `AR-15.79.3-3` ("argument-2 shall have the same type as argument-1"), which a
>   per-position screen cannot express;
> · the "all arguments of the same class" rules `AR-15.71.3-3` / `AR-15.72.3-3`, and the zero-length-literal
>   rules `AR-15.71.3-2` / `AR-15.72.3-2`, all still DIVERGES.
>
> **GOLDENS** (all four registered in `tests/conformance/negative/manifest.json` in the same commit):
> `pb1-numeric-arg-alphanumeric` · `pb1-string-arg-numeric` (the two hand reproductions) ·
> `pb1-numeric-arg-numeric-edited` (§8.5.2.1 Table 2 — numeric-edited-when-display is class ALPHANUMERIC) ·
> `pb1-numeric-arg2-alphanumeric` (REM's argument-2; a screen that stopped at the first argument would pass every
> other fixture and still miss that rule's second half).
>
> **The guard that keeps it wired:** `IntrinsicArgumentClassDriftTests` — the screen is called, every verified
> rule cites a §, every verified function is really in the catalog. It caught itself going blind on its first run
> (its row-parsing regex required digits for the arity bounds; a variadic row writes `inf`, so every variadic
> function was skipped and ORD-MAX read as absent from the catalog).
>
> The original finding follows, for provenance.

### PB1 (as found) · the intrinsic ARGUMENT-CLASS table is declared 79 times and read zero times

> **ONE architectural defect presenting as at least 12 separate rule violations.** Found by the first Phase-B
> verdict batch (§15.7 + §15.70–15.79, 55 rules over 11 functions), and it is the reason that batch returned 18
> DIVERGES — most of them are not independent bugs.
>
> **THE DEFECT.** `IntrinsicCatalog.cs` gives every one of its **79** catalog rows an `ArgKinds` string declaring
> each argument's required class (`"n"` numeric · `"s"` string · `"i"` integer · `"p"` polymorphic), and exposes
> `IntrinsicSig.ArgKind(int i)` to read it. **`ArgKind` has zero callers.** The only read of `ArgKinds` anywhere
> in `src/` is one equality test:
>
> ```
> src/Cobol.Net.Compiler/Binding/Procedure/Verbs/IntrinsicBinder.cs:267
>     if (sig.ArgKinds == "p" && args.Count > 0 && args.All(IsStringOperand))
> ```
>
> — the MAX/MIN category-polymorphism switch. So the table that exists precisely to enforce §15's argument rules
> enforces nothing, and argument-class checking survives only as hand-written per-function arms for a handful of
> functions (`CheckRepertoireArgs` for DISPLAY-OF/NATIONAL-OF, `BindConvert`, CHAR, the ALGEBRAIC family). **One
> rule, written in a few places and declared-but-unread in all the others** — the shape DA3/DA5/DA6 recorded as
> `feedback_one_rule_one_place`, this time with the general mechanism already built and simply not wired in.
>
> **⛔ REPRODUCED BY HAND, both directions, at the CLI — not taken from an agent's report:**
> ```cobol
> 01 N PIC 9(4) VALUE 1234.   01 R PIC X(10).
>     MOVE FUNCTION REVERSE(N) TO R        *> §15.78.3 r1: alphabetic/alphanumeric/national ONLY
> ```
> compiles clean and displays `4321      `. And the mirror:
> ```cobol
> 01 A PIC X(4) VALUE "ABCD". 01 R PIC S9(6)V99.
>     COMPUTE R = FUNCTION ABS(A)          *> §15.7.3 r1: argument-1 shall be of class numeric
> ```
> compiles clean and displays `0000000{` — garbage from coercing `"ABCD"` through `CobolNum.FromAlphanumeric`.
> Neither emits a diagnostic at any edition.
>
> **⚠ AND IT IS A HOLE IN DA6, WHICH LANDED HOURS EARLIER.** DA6 installed the §8.8.1.1 strict reject for an
> alphanumeric ARITHMETIC operand (`COBOLNET0844`). Function arguments bypass it: `BindArgOperand` routes through
> `BindFunctionArgumentExpr`, whose `OperandContext.FunctionArgument` deliberately suppresses the
> `ExpressionBinder.OperandRef` numeric screen. So `COMPUTE R = A` is correctly rejected while
> `COMPUTE R = FUNCTION ABS(A)` is not. Sweeping DA6's siblings is part of this fix (`feedback_scan_all_similar`).
>
> **THE FIX IS THE WIRING, NOT TWELVE PATCHES** (CLAUDE.md rule 5 — prefer the shape that makes the next case
> automatic). Consume `sig.ArgKind(i)` in `IntrinsicBinder.BindIntrinsicCore` as the general per-argument class
> screen for every catalogued function, with the existing hand-written arms folded into it or kept only where a
> rule is genuinely function-specific. Pair it with a drift test asserting that every catalog row's declared
> `ArgKinds` is actually consulted — otherwise the table can go dead again exactly as it did.
>
> **SCOPE — this is bigger than the 12 rows that found it.** §15 holds **216 AR rules across 43 functions**, of
> which **47 are explicit class/category constraints**; only 11 functions have been adjudicated so far. Expect the
> remaining §15 clauses to return the same verdict, and expect the count to grow as Phase B proceeds. The
> VALUE-constraint rules ride along: §15.3 requires a bad argument VALUE to set **EC-ARGUMENT-FUNCTION**
> (`FUNCTION REM(x 0)`, `FUNCTION PRESENT-VALUE(-1 …)`, `FUNCTION RANDOM(-1)`, a zero-length literal to ORD-MAX),
> and none of those raises today either.
>
> **INSTANCES RECORDED IN THE INVENTORY** — 11 rows (each carries its own §, code-location and reproduction):
> `AR-15.7.3-1` ABS · `AR-15.70.3-1` ORD · `AR-15.71.3-1`/`-3` ORD-MAX · `AR-15.72.3-1`/`-3` ORD-MIN ·
> `AR-15.74.3-1` PRESENT-VALUE · `AR-15.75.3-1` RANDOM · `AR-15.76.3-1` RANGE · `AR-15.77.3-1` REM ·
> `AR-15.78.3-1` REVERSE · `AR-15.79.3-3` SECONDS-FROM-FORMATTED-TIME.
> The **zero-length-literal** rules ride the same dead table from the other side: `AR-15.71.3-2` /
> `AR-15.72.3-2` note the guard exists for CONVERT and the repertoire functions and for nothing else.
>
> ⚠ **These rows are AGENT-SURFACED and adversarially re-verified, but only the two reproductions above and the
> zero-callers fact were confirmed by hand.** Design doc §7 stands: verify each before it drives a code change.
> All are DIVERGES/PARTIAL, which do NOT close a GAP, so nothing in the burn-down rests on them.

### PB2 · [MAJOR] · intrinsics · ✅ LANDED — the ARGUMENT path (DEVLOG 1118); the RECEIVER residue stays open

> **The renderer now routes on the ARGUMENT's type, not only on the function's family** — one line in
> `IntrinsicRenderer.RenderNum`, plus a floating-point body per exact-family function in
> `CobolIntrinsics.RealArgs.cs`. Five rows close (ABS, ORD-MAX, ORD-MIN, RANGE, REM); GAP 3779 → 3774.
>
> **⛔ IT WAS WORSE THAN THIS ENTRY SAID.** Not "no value, a CS1503, or a silent requantization" as three
> possibilities — for the exact family it was reliably a **raw Roslyn error escaping the compiler on legal
> COBOL**: `CS1503: cannot convert from 'double' to 'System.Int128'`, quoting generated C# the user never asked
> to see. Ten of eleven functions probed did it. That is an INTERNAL failure surfaced as a diagnostic, which is
> worse than a wrong answer.
>
> **⚠ THE FIRST FIX WAS WRONG AND THE CORPUS CAUGHT IT.** The elegant form — give the real bodies the SAME names
> as the exact ones, since `Int128` has no implicit conversion from `double`, and let one dispatch line do
> everything — does not compile. An integer LITERAL converts implicitly to BOTH, so `FUNCTION MAX(5 7)` emitted
> `MaxScaled(5, 7)` and C# reported `CS0121: ambiguous call`, **breaking six previously-green corpus programs
> that never touched a float**. The real bodies therefore carry a `…Real` name, by a CONVENTION rather than a
> table (`XxxScaled` → `XxxReal`, else a `Real` suffix — one string transform in `IntrinsicRenderer.RealMethod`).
>
> **AND THE DRIFT TEST FOUND A GAP THE PROBE MISSED**: `COMBINED-DATETIME`, whose argument-2 §15.6 types `Num2`
> and which may therefore legitimately be a float. Its body is `argument-1 + (argument-2 / 100000)` exactly as
> §15.17.4 r1 writes it (the exact twin encodes the same expression as a scale shift, so the two agree by
> construction).
>
> **RESIDUE — the rows this did NOT fix, each still recorded PARTIAL:** `RV-15.75.4-1` RANDOM, whose defect is
> the fixed-point RECEIVER path (`FromDouble(call, ws)` re-rounding a value §15.75.4 r1 already places in
> `[0,1)`), not the argument path; the standard-arithmetic legs `RV-15.73.3-2` / `-3` (PI under
> standard-binary/standard-decimal) and `RV-15.74.4-1`; and the EC-ARGUMENT-FUNCTION **value** rules
> (`AR-15.74.3-2`, `AR-15.75.3-2`, `AR-15.77.3-2`), which §15.3 makes a RUN-TIME condition and which this
> compile-time change deliberately does not touch.
>
> **GOLDEN:** `conformance:2023/pb2_float_argument_exact_family` — 17 lines, every expected value derived from
> the spec, built around the pairs that distinguish a correct body from a plausible one (MOD floors where REM
> truncates; INTEGER floors where INTEGER-PART truncates). It matched on the first run.
>
> The original finding follows, for provenance.

### PB2 (as found) · a FLOATING-POINT argument falls off the end of the intrinsic result path

> **19 of the batch's 42 open rows cluster here** — the second pattern behind the same 11-function sample, and
> independent of PB1. Where PB1 is "no argument-class rule is enforced", this is "an argument of a class the rule
> ALLOWS is not handled".
>
> The intrinsic result path is written for fixed-point operands. A float argument — legal for every one of these
> functions, since §15.71.3 r1 and its siblings bar only boolean/message-tag/object/pointer/strongly-typed-group —
> variously produces no value at all, a Roslyn `CS1503`, or a silent requantization. Reported instances span
> ORD-MAX/ORD-MIN (`RenderNum`'s `OrdMax or OrdMin` arm calls the scale-aligning path), RANGE, REM, ABS with a
> COMP-2 operand, and RANDOM's fixed-point RECEIVER leg (`FromDouble(call, ws)` re-rounds a value the spec says is
> already in `[0,1)`). PI's `standard-binary` / `standard-decimal` rows (`RV-15.73.3-2`, `-3`) are the same seam
> seen from the arithmetic-mode side, as is PRESENT-VALUE's `RV-15.74.4-1`.
>
> **Do not fix these one function at a time.** The shape of the defect is a missing branch in ONE renderer seam,
> so the fix belongs there, with the per-function rows as its verification set. Rows: `RV-15.7.4-1`,
> `RV-15.71.4-1`/`-2`, `RV-15.72.4-1`/`-2`/`-3`, `RV-15.73.3-1`/`-2`/`-3`, `AR-15.74.3-2`, `RV-15.74.4-1`,
> `AR-15.75.3-2`, `AR-15.75.3-4`, `RV-15.75.4-1`/`-3`, `RV-15.76.4-1`, `AR-15.77.3-2`, `RV-15.77.4-1`,
> `RV-15.78.4-1`.
>
> ⚠ Agent-surfaced, adversarially re-verified, NOT hand-confirmed. Verify before it drives a code change.

### PB6 · [MAJOR] · interprogram · ✅ LANDED (DEVLOG 1126) — CALL BY VALUE quoted §8.8.1.1 at a programmer who broke §14.9.4.3 SR22

> Found by the pre-merge GnuCOBOL differential, which no other gate leg could have caught: the VERDICT was right,
> so nothing failed anywhere — only the rule quoted was wrong. `CALL … USING BY VALUE <alphanumeric>` was refused
> by DA6's §8.8.1.1 ARITHMETIC screen because the grammar production is named `arithmeticExpression` and the
> binder called `BindExpr` on it. §14.9.4.3 SR22 is the governing rule ("identifier-4 shall be of class numeric,
> object, or pointer"), so the operand IS illegal — but a diagnostic naming the wrong clause sends the programmer
> to the wrong place. **A production's NAME is not its operand's rule.**
> `COBOLNET1628` + `OperandContext.CallByValue`; golden `pb6-call-by-value-alphanumeric`.
> ⚠ **The first fix was a SILENT REGRESSION that looked like success**: screening the wrapper via
> `IntrinsicArgumentRules.ClassOf` made both cases compile CLEAN, because that method maps any
> `BoundComputedOperand` to NUMERIC. A wrongly-worded reject had become no enforcement at all. **A fix whose
> evidence is "the error went away" cannot distinguish a fix from a deletion.**

### PB7 · [BLOCKER] · intrinsics · ✅ LANDED (DEVLOG 1129) — every ZERO-ARGUMENT intrinsic was unreachable in the keyword-omitted form, and it compiled clean

> **Silent compile-then-crash, the worst failure mode this review has produced.**
> ```cobol
> REPOSITORY. FUNCTION ALL INTRINSIC.
>     MOVE CURRENT-DATE TO WS-CD      *> compiles with zero diagnostics
> ```
> then at RUN TIME: `NotImplementedCobolFeatureException: reference 'CURRENT-DATE'`. `PI` and `E` failed
> identically — the whole zero-argument family.
>
> §15.21.2's general format is `FUNCTION CURRENT-DATE` with NO parentheses, so with the keyword omitted
> (§12.3.8.1 + §8.4.3.2.3 SR2) the reference is a **bare name — ZERO suffixes, not one**.
> `IntrinsicBinder.KeywordOmittedFunction` opened `if (suffixes.Length != 1 …) return null;`, so it fell through
> to a data reference, resolved to nothing, and reached the runtime's not-implemented stage. The standard writes
> the form itself at §D.14.3.6: `MOVE FUNCTION LOCALE-DATE (CURRENT-DATE (1:8))`.
>
> **Fixed narrowly.** A bare name becomes a function reference ONLY when the catalog says the function admits
> zero arguments (`MinArgs == 0`), so a declared data item still wins and no other bare word is re-routed for
> merely sharing a name with a function. Verified both directions.
> **GOLDEN** `conformance:2023/pb7_keyword_omitted_zero_arg`.

### PB9 · [MAJOR] · intrinsics · ✅ LANDED (DEVLOG 1133) — a RESERVED intrinsic name in the KEYWORD-OMITTED form

> **⛔ THE SCOPE IN THIS ENTRY WAS WRONG — IT IS FOUR WORDS, NOT ONE — AND THE MEASUREMENT THAT CORRECTED IT ALSO
> FOUND A TRANSCRIPTION DEFECT.** The entry said "measured scope is exactly one word (RANDOM)". That sweep covered
> the nine ZERO-ARGUMENT intrinsics, which structurally could not see SIGN or SUM because they take arguments.
> The set that matters is **§8.9 reserved words ∩ §8.11 intrinsic function names** — a reserved word cannot be a
> data name (§8.3.2.4.1), so it can never arrive through the ordinary keyword-omitted route — and that
> intersection is **LENGTH, RANDOM, SIGN, SUM**. LENGTH already worked (it is in the `cobolWord` name slot for
> `START WITH LENGTH`); the other three were rejected.
> ⚠ The first intersection returned 88 names because **§8.11's transcribed list ran six names together on one
> line** (`SUM TAN TEST-DATE-YYYYMMDD …`) — a PAGE-BOUNDARY join, confirmed by rendering printed folios 213/214
> where each sits on its own line. Repaired under a word-conservation assert; the list now reads 94.
>
> **⛔ TWO WRONG FIXES BEFORE THE RIGHT ONE, both caught by NIST NC116A, both the SAME collision:**
> ```
> 01  WRK-DS-LS-5 PICTURE S99999   VALUE ZERO
>         SIGN LEADING SEPARATE.        ->  COBOLNET1585 "takes exactly one literal"
> ```
> · **Attempt 1 — `nameSlot: true`** (copying LENGTH). A name-slot word is admissible as a VALUE-clause literal,
>   and the operand loop is greedy, so it swallowed the SIGN CLAUSE of the next line as a second literal. LENGTH
>   is safe there only because LENGTH does not BEGIN a data-description clause; SIGN (§13.18.53) and SUM (§13.14)
>   both do.
> · **Attempt 2 — a dedicated `functionCall` alternative**, described in its own comment as "confined to
>   expression positions, so it cannot reach a data description". It reaches one in a single hop:
>   `valueClauseOperand : unaryExpression` → `primaryExpression` → `functionCall`. Identical failure. Confinement
>   was ASSERTED rather than traced.
>
> **THE FIX IS DERIVED FROM THE FUNCTIONS' OWN GENERAL FORMATS, NOT GUARDED.** §15.81.2 writes
> `FUNCTION SIGN ( argument-1 )` and §15.88.2 `FUNCTION SUM ( { argument-1 } … )` — neither has a no-argument
> form, so a BARE `SIGN` or `SUM` is never a function reference. Requiring the argument group makes
> `SIGN LEADING` unmatchable as a functionCall, so the collision is **structurally impossible** rather than
> defended against, and the report-writer `SUM OF` clause is covered by the same fact. RANDOM keeps its bare form
> (§15.75.2 brackets the whole parenthesised part) and begins no data-description clause.
> **The argument carrier came from a TOKEN DUMP:** these words carry `subscriptTrigger=true`, so with no FUNCTION
> keyword the lexer pushes SUBSCRIPT mode — `SUM(1 2 3)` lexes `SUM LPAREN SUB_INTEGERLIT×3 SUB_RPAREN` — so the
> alternative takes a `subscriptPart` and re-parses through the ONE D2 `ReparseArgs` path. Pinned by
> `CobolLexerModeDriftTests`.
> **GOLDEN** `conformance:2023/pb9_reserved_intrinsic_names` — all four words in BOTH reference forms, asserted
> equal, plus the NC116A construct itself so the regression that cost two attempts is pinned inside the test.
> **NEGATIVE** `pb9-reserved-fn-no-repository` — §8.4.3.2.3 SR2: without a REPOSITORY declaration the omission is
> not permitted, and since a reserved word cannot be a data name the rejection is unambiguous.
>
> ---
> *The original entry follows, kept because the scope correction is only legible beside it.*

### PB9 (as originally filed) · `RANDOM` cannot be written in the KEYWORD-OMITTED form at all

> **We reject legal COBOL** (the CLAUDE.md rule 4 red line), and it is the last survivor of PB7's family.
> ```
> REPOSITORY. FUNCTION ALL INTRINSIC.
> COMPUTE N = RANDOM        ->  COBOL0001: no viable alternative at input 'COMPUTEN=RANDOM'
> MOVE RANDOM TO T          ->  COBOL0001: no viable alternative at input 'MOVERANDOM'
> ```
> ✅ **§8.4.3.2.3 SR2** permits omitting FUNCTION for any REPOSITORY-declared intrinsic, and **§15.75.2**'s
> general format brackets the whole parenthesised part, so a bare `RANDOM` is a legal zero-argument reference.
>
> **MEASURED SCOPE — exactly one word.** A sweep of all nine zero-argument intrinsics in the keyword-omitted
> form: CURRENT-DATE · WHEN-COMPILED · PI · E · EXCEPTION-STATUS · EXCEPTION-LOCATION · EXCEPTION-STATEMENT ·
> SECONDS-PAST-MIDNIGHT all bind (PB7); **RANDOM alone fails.**
>
> **ROOT CAUSE — a DIFFERENT one from PB7's**, which is why PB7's fix did not reach it. PB7 was about suffix
> ARITY in `KeywordOmittedFunction`; this is about TOKENIZATION. `RANDOM` lexes as its own reserved-word token,
> so `MOVE RANDOM` never reaches `dataReference`/`cobolWord` and the keyword-omitted router is never consulted.
> The fix is a `cobol-words.json` row, so it carries the `new-construct` skill's mandatory edition-gate sweep —
> deliberately NOT folded into PB8, whose root cause it does not share.
> ⚠ **Not a regression:** verified identical on the pre-PB8 compiler by stashing the change.

### PB8 · [MAJOR] · reference-modification · ✅ LANDED (DEVLOG 1131) — reference-modifying a FUNCTION result

> **⛔ THE ROOT CAUSE RECORDED BELOW WAS WRONG, AND THE CORRECTION IS THE VALUABLE PART.** This entry said the
> defect was a LEXER-MODE decision and that the fix was "a lexer-mode change PLUS a `functionCall` grammar tail —
> the riskiest category in this codebase". **A token dump refuted it before a line was written** (the entry was
> reasoned from `OnDefaultLParen`'s source, never measured): in BOTH keyword-present shapes the ref-mod paren is
> **already lexed in DEFAULT mode** — after a function NAME the lexer's own FUNCTION suppression keeps it out of
> SUBSCRIPT mode, and after the argument list's `)` the previous token is not a data-name so the SUBSCRIPT
> trigger never fires. Both therefore reach the DEFAULT-mode `refModPart` that had existed all along for
> `dataReference`. **THE LEXER WAS NEVER TOUCHED.** `CobolLexerModeDriftTests` now pins that per shape, so the
> conclusion cannot rot. `use_antlr_tree_dump` is in memory for exactly this reason — the budgeted risk was an
> artifact of not having dumped.
>
> **THE DEFECT WAS ALSO WIDER THAN THE TWO REPROS**, and the widest case was the one nobody had written down:
> | shape | before |
> |---|---|
> | `FUNCTION CURRENT-DATE (1:4)` | COBOL0001 parse error |
> | `FUNCTION UPPER-CASE("abc") (1:2)` | COBOL0001 parse error |
> | `CURRENT-DATE (1:8)` (keyword-omitted) | COBOLNET1543 — the group was read as an ARGUMENT LIST |
> | `UPPER-CASE("abc") (2:3)` (keyword-omitted) | ⛔ **COMPILED CLEAN, threw at RUN TIME** — the PB7/DA7 wrong-stage family |
> | `FUNCTION LOCALE-DATE(CURRENT-DATE (1:8))` — **the standard's own §D.14.3.6 example** | COBOLNET1543 |
>
> ⚠ **AND THE GRAMMAR TAIL ALONE WOULD HAVE MADE IT WORSE.** With `refModPart` parsing but the binder ignoring
> it, `FUNCTION UPPER-CASE("abcdefgh") (2:3)` compiled and printed **ABC** instead of BCD — the ref-mod silently
> DROPPED. A loud parse error had become a silent wrong answer. Measured before proceeding, not after.
>
> **THE FIX, and where each rule lives:**
> · grammar — `functionCall : FUNCTION functionName (LPAREN functionArgList? RPAREN)? refModPart*`;
> · §8.4.3.3.3 SR2 (alphanumeric/boolean/national only) — `IntrinsicBinder.ResultRefMod`, **`COBOLNET1629`**;
> · §8.4.3.2.3 SR6 (a `(` after an argument-PERMITTING function name is ALWAYS the argument list — the
>   standard's own RANDOM trap) — same site, reported as the argument-list error it is (`COBOLNET1543`), not as
>   a class error about a ref-mod that was never written;
> · §8.4.3.3.3 SR3 (no ref-mod of a ref-mod) — **`COBOLNET1630`**, counted in the binder rather than expressed as
>   the grammar's arity, so the function and data-reference sides report the SAME rule;
> · the two source carriers (parsed `refModPart` · SUBSCRIPT-mode capture) now reduce through ONE reader,
>   `ReferenceResolver.ReadRefMod` → `RefModSpec`, and the slice reuses `CobolString.RefMod` — so the
>   §8.4.3.3.4 item-5c bounds check and EC-BOUND-REF-MOD are shared, not re-implemented.
> **A RIDER, NOT A WRAPPER:** the ref-mod hangs on `BoundIntrinsicCall.RefMod` because the alphanumeric string
> channel is selected by pattern-matching that node at several sites; a wrapper would have silently stopped
> matching at each one, and the failure mode is a DROPPED ref-mod, not a compile error.
> **GOLDEN** `conformance:2023/pb8_refmod_function_result` (9 cases incl. §D.14.3.6) + four negative fixtures.
> **INVENTORY** `SR-8.4.3.3.3-2` and `SR-8.4.3.3.3-3` CLOSED; `SR-8.4.3.2.3-6` PARTIAL (see PB9).
>
> ---
> *The original entry follows, kept because the correction above is only legible beside it.*
>
> **We reject legal COBOL — the CLAUDE.md rule 4 red line.** Both forms, hand-verified at `--std 2023`:
> ```
> MOVE FUNCTION CURRENT-DATE (1:4)      TO WS-YR  ->  COBOL0001: no viable alternative at input '('
> MOVE FUNCTION UPPER-CASE("abc") (1:2) TO T      ->  COBOL0001: no viable alternative at input '('
> ```
> ✅ **§8.4.3.3.3 SR2, validated verbatim:** "If identifier-1 is a function-identifier, it shall reference an
> alphanumeric, boolean, or national function." The standard explicitly contemplates reference-modifying a
> function-identifier and constrains only WHICH functions qualify — CURRENT-DATE (§15.21.1, alphanumeric) and
> UPPER-CASE both do. The shape is normative at §15.23.3 r5, §15.25.3 r5 and §15.100.3 r5, and written out at
> §D.14.3.6.
>
> **⛔ ROOT CAUSE IS IN THE LEXER, AND THE FILE WARNS IT CANNOT BE REPAIRED DOWNSTREAM.**
> `CobolLexer.g4`'s `OnDefaultLParen` pushes SUBSCRIPT mode — which is what captures a ref-mod — only when
> `PreviousTokenCouldBeDataName() && !PreviousIsFunctionName()`. The two failing shapes miss it for DIFFERENT
> reasons:
> · after a zero-argument function NAME, `PreviousIsFunctionName()` is TRUE, so the trigger is suppressed;
> · after a function call's closing `)`, the previous token is not a data name at all, so it never fires.
> The lexer's own comment: "the SUBSCRIPT-mode decision at '(' is frozen at lex time and cannot be repaired
> later." The fix is therefore a lexer-mode change PLUS a `functionCall` ref-mod tail in the grammar — the
> riskiest category in this codebase, and deliberately NOT attempted in the same pass that landed PB7.
>
> ⚠ Interaction worth knowing: PB7 made zero-argument keyword-omitted references bind, so the standard's own
> §D.14.3.6 example `CURRENT-DATE (1:8)` gains a second reason to work once this lands.

### PB5 · [BLOCKER] · numerics · ✅ LANDED (DEVLOG 1124) — the float→fixed quantizer saturated at an ORDINARY COBOL magnitude

> **Silent wrong arithmetic in ordinary business ranges — the worst defect this review has surfaced.**
> `CobolIntrinsics.FromDouble` returned a `long` and clamped at `long.MaxValue`; its caller quantizes at
> `ws = max(Receiver.Scale, 9)`, so the clamp bit at **|value| ≈ 9.2 × 10⁹**. Every float-family result at or
> above that magnitude was replaced by the constant **9223372036.85**.
>
> ```cobol
> 01 R PIC 9(12)V99.
>     COMPUTE R = FUNCTION ANNUITY(10000000000 1)      *> §15.9.4 r1b ⇒ exactly 1 + argument-1 = 10000000001.00
>       ON SIZE ERROR ... NOT ON SIZE ERROR ...        *> printed NO SIZE ERROR
>     R = 00922337203685                                *> wrong by 8%
> ```
> `SQRT(1e20)`, `EXP(23.3)`, `ABS` and `MAX` over a COMP-2 all produced that same constant. **A twelve-digit
> money field is routine COBOL**, so this was not an edge case; and there was no diagnostic, because §14.7.4
> never saw an overflow — the value had already been clamped to something that fits.
>
> ⛔ §15.4.1 licenses an implementor-defined **approximation** of the equivalent arithmetic expression under
> native arithmetic. **9223372036.85 is not an approximation of 10000000001.**
>
> **THE FIX IS A TYPE, AND IT IS A CORRECTNESS FIX RATHER THAN A WIDENING FOR COMFORT.** The scaled domain of
> this compiler already IS `Int128` — every `…Scaled` body takes one — and `FromDouble` was the one member of
> that pipeline still returning `long`. At scale 9 the Int128 ceiling is ≈1.7 × 10²⁹, past the 10¹⁸ any PICTURE
> can describe, so the saturation is now unreachable from a declarable receiver rather than merely further away.
> One emit site (`IntrinsicRenderer.RenderFloat`), so the change is contained.
>
> ⚠ **FOUND BY THE PHASE-B REFUTE STAGE, NOT BY THE ADJUDICATOR** — the ANNUITY reviewer overturned a PARTIAL to
> DIVERGES and produced the repro. The adjudicator had checked only small receivers (`V9(4)`, `V9(7)`), where
> the clamp never bites. That is the second time this session the adversarial pass paid for itself on a defect
> the first pass had looked straight at.
>
> **AND A CITATION DEFECT AT THE SAME SITE:** the `FromDouble` comment cited **§14.6.13.1.1 Table 13** for
> EC-ARGUMENT-FUNCTION being fatal. `cite.py --check` FAILS on it (that clause is titled "General"); the real
> one is **§14.6.13.1.6**. It had propagated from the source comment into an agent's adjudication — the
> inherited-citation shape again, and one the quoted-fragment audit could not see because it carries no quote.
>
> **GOLDEN** `conformance:2023/pb5_float_quantize_range` — pins the saturation, deliberately NOT the binary64
> cent that the §15.4.1 approximation legitimately produces on ANNUITY's division.

### PB4 · [MAJOR] · literals · ✅ LANDED (DEVLOG 1119) — a HEXADECIMAL literal was not decoded in FIVE positions

> **Found by accident, which is the part worth keeping.** It surfaced while building a test vehicle for PB3: the
> vehicle used `PIC X VALUE X"FF"` and reported `ORD = 89`. 89 − 1 = 0x58 = **'X'** — the item held the letter X,
> because the VALUE path had stored the literal's own SOURCE TEXT and truncated it to the picture. PB3's agent
> report was measured through this same distortion.
>
> **§8.3.3.2 makes a hexadecimal literal one FORM of an alphanumeric literal** — "each pair of hexadecimal digits
> represents a single character" — so every position accepting an alphanumeric literal accepts it. Five did not,
> each failing differently, and the data division disagreed with the procedure division about the same literal:
>
> | site | before | correct |
> |---|---|---|
> | `01 B PIC X(2) VALUE X"4142"` | `X"` (source text, truncated) | `AB` |
> | `01 A PIC X(4) VALUE ALL X"41"` | `ALLX` | `AAAA` |
> | `05 E OCCURS 2 PIC X(2) VALUE X"4142"` | as VALUE | `AB` / `AB` |
> | `88 IS-AB VALUE X"4142"` | never matched | matches |
> | `MOVE ALL X"41" TO M` | parsed, then a RUN-TIME `NotImplementedCobolFeatureException` | `AA` |
> | (`MOVE X"4142" TO M` decoded correctly all along) | `AB` | `AB` |
>
> **⛔ ROOT CAUSE — one rule written down twice, and BOTH copies wrong the same way.** `CobolLiteral` carried the
> prefix-letter list in `Decode` AND in `IsStringLiteral`, and neither included `X`. So a hex literal was
> simultaneously *not recognised as a literal* and *not decoded as one*, which is why the five sites failed in
> five different ways: each had built its own compensation on top of a decoder that quietly returned the input
> unchanged. Adding a `DecodeHex` arm at `ValueInitializer` would have made it the **fifth** copy of the dispatch
> DA3 already found three of. The list is now one `PrefixLetters` constant behind one `SplitLiteral` helper.
> The remaining site was `ExpressionBinder.FigurativeOperand`, whose own comment said "(ALL HEXLIT / NULL stay a
> later slice)" while the grammar had listed `ALL HEXLIT` all along.
>
> ⚠ **The X-prefix guard is load-bearing**: `DecodeHex` returns "" for anything it does not recognise, so
> delegating on a leading `X` alone would turn the ordinary word `XYZ` — which `Decode` contracts to return
> unchanged — into the empty string. The golden pins `XYZ`.
>
> **GOLDEN** `conformance:2023/pb4_hex_literal_value` — all five sites plus the guard, expected values from
> §8.3.3.2 arithmetic.

### PB3 · [MAJOR] · intrinsics · ✅ LANDED (DEVLOG 1121) — ORD skipped an ordinal position past the 256-entry collating table
> **⚠ HAND-VERIFIED AND NARROWED — the original report was measured through PB4 and is half wrong.** The ALSO
> collapse it blamed is CORRECT: under `ALPHABET AL IS "A" ALSO "B"`, `ORD("A")` = `ORD("B")` = **1** (§12.3.7
> GR L3.6 — ALSO assigns one ordinal position), `ORD("C")` = **67** and `ORD(X"FF")` = **255**, all matching the
> derivation {A,B} at 1 · 0x00–0x40 at 2–66 · 0x43–0xFF at 67–255. Nothing there is wrong.
> **What IS wrong is the character past the table:** `ORD(U+0100)` = **257**, so position **256 is occupied by
> nothing** while 255 and 257 are. `CobolIntrinsics.Text.Ord` is
> `c < weights.Length ? weights[c] + 1 : c + 1` — the fallback ignores the collating sequence entirely, so a
> character one past the 256-entry table is numbered by a different rule than its neighbour.
> ⛔ **The original citation is FABRICATED — "§12.3.7 GR7 k3 … distinct ascending" — and it came from our own
> design doc, not from the agent.** `cite.py --find "distinct ascending"` returns *"NO CLAUSE contains 'distinct
> ascending'"*: the phrase is nowhere in the standard. `COBOLNET_DESIGN.md` carried it twice and the Phase-B agent
> inherited it from there. Exactly the failure mode CLAUDE.md rule 1 names — not invented, INHERITED. The design
> doc is corrected in the same change set.
>
> ✅ **THE REAL RULE IS §12.3.7.4 GR7 1.3, and it is STRONGER than the fabrication** (`cite.py --check` OK):
> *"Any characters of the native collating sequence that are not specified in the literal phrase shall assume a
> position in the collating sequence that is greater than that of the highest character specified in this literal
> phrase. The relative order within the set of these unspecified characters is unchanged from the native
> collating sequence."*
>
> ⚠ **This CORRECTS what this entry first said.** It claimed the value for U+0100 was "under-specified" and only
> the absence of a gap was provable. Not so: GR7 1.3 DETERMINES it. Unspecified characters sit above the highest
> specified one in unchanged native relative order, and U+0100 immediately follows U+00FF in that order, so with
> U+00FF at 255 the answer is **256** — and 257 is wrong for a citable reason, not merely an inconsistent one.
> The observed 255 for `X"FF"` and 1 for both ALSO characters are likewise GR7 1.3 and L3.6 exactly.
>
> ✅ **FIXED BY GIVING THE ALPHANUMERIC SIDE THE ARITHMETIC THE NATIONAL SIDE ALREADY HAD** —
> `NationalCollation.Weight` computes the unspecified tail as `nextFree + (c − |specified below c|)`, precisely
> GR7 1.3, and has done all along. `CobolIntrinsics.Text.Ord(string, ushort[])` now continues the sequence above
> the highest tabulated position instead of falling back to `c + 1`. One rule, two implementations, and only one
> of them was incomplete (`feedback_one_rule_one_place`).
> ⚠ **It did NOT need the data-structure change this entry predicted.** The dense 256-entry array is fine: the
> table's own maximum position is all the arithmetic needs, so the repair is local to `Ord`. The estimate was
> wrong in the cheap direction for once — recorded because a scope guess that reads as authoritative is how a
> fix gets deferred for being "big".
> Measured after the fix, all six spec-derived: `ORD("A")`=`ORD("B")`=1 · `ORD("C")`=67 · `ORD(X"FF")`=255 ·
> **`ORD(U+0100)`=256** · `ORD(U+0101)`=257. **CA26's residue on the ORD path is closed.**
> **GOLDEN** `conformance:2023/pb3_ord_collating_tail`.

### PB3 (as found) · ORD reports the wrong ordinal under a custom PROGRAM COLLATING SEQUENCE

> Found by the adversarial pass OVERTURNING a CONFORMS, which is the clearest evidence that pass earns its cost.
> `RV-15.70.4-1` — two independent failures, each compiled and RUN at `--std 2023`:
> · **ALSO collapse.** With `ALPHABET AL IS "A" ALSO "B"` and `PROGRAM COLLATING SEQUENCE AL`, `ORD(X"FF")`
>   printed `00255` and `ORD(U+0100)` printed `00257` — skipping position 256. §12.3.7 GR7 k3 gives unspecified
>   characters *distinct ascending* positions with no gap. The correct arithmetic already exists on the NATIONAL
>   twin, which returns the right value for the identical alphabet shape — one rule, two implementations, one of
>   them wrong (`feedback_one_rule_one_place`).
> · **>255 masking.** `CobolIntrinsics.Text.Ord(string, ushort[] weights)` is
>   `c < weights.Length ? weights[c] + 1L : c + 1L`, so any code unit past the 256-entry table bypasses the
>   collating sequence entirely. With a custom PCS, EVERY character then reports a wrong ordinal.
>
> ⚠ This directly contradicts CA26 ("the alphanumeric repertoire is UNICODE … no longer 8-bit-aliased", DEVLOG
> 1018), so CA26's fix is incomplete on the ORD path. Agent-surfaced and run, not hand-confirmed by me.

## 🔎 DISCOVERED DURING IMPLEMENTATION (not part of the original 46 audit set) — ALL LANDED

### DA7 · [MINOR] · inspect-string · ✅ LANDED (DEVLOG 1108) — three syntax-rule violations moved from RUN TIME to COMPILE time
> **New diagnostic `COBOLNET1626` (`character-operand-usage`), edition-invariant.** All three rules are unchanged at
> 85/2002/2014/2023, so the diagnostic is deliberately NOT gated and no introduction axis applies — verified firing
> at all four editions. The verdicts were already correct; only the STAGE moved.
> · `InspectBinder.cs` — §14.9.22.3 SR1, an elementary non-display/national identifier-1. Was a pure staging
>   choice: the check already ran in the binder and merely returned `BoundUnsupported`.
> · `StringUnstringBinder.cs` (UNSTRING) — §14.9.48.3 SR4. Same shape; the check already existed in the binder.
> · `StringUnstringBinder.cs` (STRING) — §14.9.43.3 SR1. This one had to be ADDED to the binder: it existed only
>   in `StringEmitter` as a run-time loud stage, so `STRING … INTO <a COMP item>` compiled clean and crashed.
> **⛔ THE FALSE-POSITIVE CHECK IS THE POINT OF THIS CHANGE**, since adding a diagnostic risks rejecting legal
> source. Verified: the three illegal forms now give `COBOLNET1626` at compile time, while ALL of
> `INSPECT <alnum>`, `STRING/UNSTRING INTO <group>` (including a COMP-leaf group — DA5), and
> `STRING/UNSTRING INTO <numeric DISPLAY>` still compile and run. **A GROUP receiver is exempt by design**:
> usage is an ELEMENTARY property, and §14.9.43.4 GR3a transfers into STRING's receiver "in accordance with the
> MOVE statement rules for alphanumeric-to-alphanumeric moves", which admit a group. §14.9.22.3 SR1 says so
> outright, naming "an alphanumeric or national group item" and constraining only an elementary operand.
> Negative fixtures `da7-inspect-binary-operand` · `da7-string-into-binary` · `da7-unstring-into-binary`, each
> asserted to reject at all four editions. `docs/DIAGNOSTICS.md` regenerated — the registry drift test caught the
> stale doc and named the generator, which is that gate working.

### DA7 (original entry, for provenance) — three syntax-rule violations diagnosed at RUN TIME, not COMPILE time
- **Sites:** `Binding/Procedure/Verbs/InspectBinder.cs:35` (INSPECT identifier-1 of USAGE BINARY — §14.9.22.3 SR1) ·
  `CodeGen/Verbs/StringEmitter.cs:168` (STRING INTO an ELEMENTARY usage-binary receiver — §14.9.43.3 SR1 requires
  usage display or national) · `StringEmitter.cs:226` (the UNSTRING counterpart, §14.9.48.3 SR4).
- **What is and is not wrong.** Each construct is GENUINELY ILLEGAL, so no conforming program is rejected and the
  compiler is not unconforming in its *verdict*. The defect is the STAGE: a syntax-rule violation should be a
  compile-time diagnostic, and instead the program compiles clean and throws when control reaches the statement.
  A user gets a run-time crash where the standard promises a compile error.
- **Cheapest at `InspectBinder.cs:35`, which is already IN THE BINDER** — it stages a `BoundUnsupported` where a
  diagnostic is immediately available, so that one is a pure staging choice with no plumbing to add.
- **Needs:** a diagnostic code (next free `COBOLNET1626`), the four-edition gate sweep, and negative fixtures.
  Separated from DA5 because DA5 was about a stale PREDICATE; this is about WHERE a correct verdict is reported.

### DA6 · [MAJOR] · arithmetic · ✅ LANDED (DEVLOG 1110) — an ALPHANUMERIC arithmetic operand, in all three shapes
> Heading corrected: the body below was updated to LANDED while this line still said OPEN. In a register the
> project treats as live-state SSOT that is a defect in itself, so it is fixed here rather than left to be
> read as the truth.
- **Spec:** §8.8.1.1 (`cite.py`-verified) — "An arithmetic expression may be an identifier referencing a **numeric
  data item**, a numeric literal, the figurative constant ZERO …". A group item is class **alphanumeric** (§8.5), so
  a group is NOT a permissible arithmetic-expression operand. `COMPUTE R = G + 1` is ILLEGAL SOURCE.
- **Observed (2026-07-29), and the two halves disagree — which is the tell:**
  · `01 G. 05 A PIC X(2) VALUE "12". 05 B PIC X(2) VALUE "34".` → `COMPUTE R = G + 1` **compiles and computes
    `R = 001235`** (the image decoded through `CobolNum.FromAlphanumeric`).
  · `01 G. 05 A PIC 9(2) VALUE 12. 05 B PIC 9(2) VALUE 34.` → the same statement **compiles and then THROWS**
    `numeric use of group item 'G'` at run time (`NumericRenderer.cs:186`).
  So the group whose digits are *unambiguous* fails, and the one whose content is merely *textual* succeeds — the
  opposite of intuition, and neither is a compile-time rejection.
- **⛔ NOT a V59 predicate residue.** Migrating `NumericRenderer.cs:186` to `IsImageCapable` would make the second
  case silently compute too — i.e. it would extend acceptance of illegal source rather than fix anything. That is
  why that one site is deliberately LEFT on `IsCharacterImage`, with a note in
  `V59ImagePredicateDriftTests` saying so.
- **✅ OWNER DECISION TAKEN (2026-07-29): reject at COMPILE time under strict conformance, with the leniency
  DIALECT-GATED behind `--permissive`** — the standing rule that every leniency is dialect-gated.
- **⛔ SCOPE IS WIDER THAN THIS ENTRY ORIGINALLY SAID, and the decision was taken on the narrower premise.**
  §8.8.1.1 bars every ALPHANUMERIC arithmetic operand, not just a group. Measured, all three forms are accepted
  today: `COMPUTE R = X + 1` with `X PIC X(4) VALUE "0012"` → **13**; `COMPUTE R = X(1:2) + 1` (a ref-mod slice,
  alphanumeric per §8.4.2.4) → **1**; and the group → **35**. `NumericRenderer` has FOUR `FromAlphanumeric` arms
  (literal · ref-mod · group · elementary alphanumeric/national), so rejecting only the group half would create a
  FRESH inconsistency of exactly the kind DA5/DA6 exist to remove. ⚠ Note the elementary arm's own comment cites
  §14.9.25.4 GR6 as justification — that is the MOVE rule, not the arithmetic-operand rule; a MOVE citation cannot
  license an arithmetic operand.
- **✅ LANDED (DEVLOG 1110) — strict rejects, `--permissive` accepts CONSISTENTLY.** The rule is enforced for ALL
  THREE alphanumeric shapes (group, elementary alphanumeric/national, reference-modified), reusing
  **COBOLNET0844** — which already IS "not a numeric operand (ISO §8.8.1.1)". Edition-invariant, verified at
  85/2002/2014/2023. Under `--permissive` both group kinds now decode identically (`R=001235`), which is the
  inconsistency this entry existed to remove; `NumericRenderer`'s group arm moved to `IsImageCapable` in the SAME
  change set, correct only paired with the rejection. `ArithmeticOperandClassTests` owns the permissive-leniency
  and consistency facts a reject-only fixture cannot express, plus the FALSE-POSITIVE guard; negative fixtures
  `da6-group-numeric-operand` / `da6-alphanumeric-numeric-operand` / `da6-refmod-numeric-operand`.
- **⛔ THE DESIGN THE FIRST ATTEMPT PROVED NECESSARY.** The rule is context-sensitive and the leaf cannot infer the
  context, so the operand context TRAVELS BY PARAMETER — the same discipline the render-side receiver follows
  (P7 Step 3: "never mutable context state"). An `OperandContext` ENUM, not a bool: the public surface is two
  intention-revealing entries, `BindExpr` (arithmetic, the default for all ~36 existing callers) and
  `BindFunctionArgumentExpr` (the ONE opt-out), over one private `BindExprCore` that threads the enum. A bool would
  have read as `BindExpr(node, true)`, would not survive a third context, and as an OPTIONAL parameter silently
  breaks the method-group conversions this spine is used through (`Select(host.Expr.BindExpr)`) — which is exactly
  how the first attempt failed to compile.
- **🔴 THE FIRST ATTEMPT AND WHY IT FAILED — kept, because the measurement is what produced the design.** Putting the check at
  `ExpressionBinder.RefExpr` (the natural-looking site: it is where a resolved data reference becomes a numeric
  expression operand, and the neighbouring `NonNumericConstantExpr` already raises **COBOLNET0844** for this very
  clause) produced **79 conformance failures** — and NOT from programs abusing the extension. The casualties were
  `FUNCTION TRIM(S)`, `SUBSTITUTE`, `FIND-STRING`, `CONVERT`, `RefModArgument_Renders`: **legal alphanumeric
  ARGUMENTS to string intrinsics** (§15.3), which §8.8.1.1 does not govern at all.
  **The root cause is structural and is the real finding:** `RefExpr` is documented as "The ONE
  dataReference→`BoundExpr` mapping, used by every expression path", so it is CONTEXT-FREE — it cannot distinguish
  an arithmetic operand from a string-function argument. A context-sensitive syntax rule cannot live there.
- **What the fix actually requires:** the check must sit where an ARITHMETIC-EXPRESSION operand is assembled, which
  means threading an operand-context (arithmetic vs. intrinsic-argument vs. reference-modifier) through the
  expression binder — a design change to the spine, not a guard. That is the work; the predicate is trivial.
- **⛔ AND `NumericRenderer.cs:186` MUST MOVE TO `IsImageCapable` IN THE SAME CHANGE SET, NEVER BEFORE IT.** Under
  the rejection, that arm becomes reachable only via `--permissive`, where the leniency must be CONSISTENT for both
  group kinds (today a `PIC X`-leaf group computes and a `PIC 9`-leaf group throws). Migrating it while strict still
  ACCEPTED the construct would extend acceptance of illegal source instead of fixing anything. Verified during the
  attempt: paired, strict rejects both group kinds and `--permissive` computes both as `001235`.

### DA5 · [MAJOR] · data-model · ✅ LANDED — V59's image predicate was migrated at SOME emit guards and not others
- **The two predicates.** V59 added `DataItem.IsImageCapable` (a BINARY/PACKED leaf HAS a pinned byte image, so a
  group containing one qualifies; only float / COMP-5 / INDEX are genuinely imageless) beside the pre-V59
  `DataItem.IsCharacterImage` (a COMP/binary leaf qualifies only via `StoreAsImage` promotion). `TierCIsland`
  documents the pair as deliberate P1/P2 — that was true BEFORE V59.
- **Why the split is now a defect and not a design.** `RecordStructEmitter.cs:123` emits `AsImage()`/`FromImage()`
  for exactly `IsImageCapable`, so the codec EXISTS for a plain COMP group. A guard still on `IsCharacterImage`
  therefore loud-stages a construct whose codec was actually generated. `MoveEmitter` ends up using BOTH
  predicates, which is the tell that this is an unfinished migration rather than a distinction.
- **✅ THE CALL HALF IS LANDED (DEVLOG 1105).** `01 G. 05 N PIC S9(4) COMP. 05 P PIC 9(3) COMP-3. 05 A PIC X(3).`
  answered `BYTE-LENGTH(G) = 7` and then threw *"no whole-group character image"* on `CALL "SUB" USING G`. That
  claim was false and refusing the CALL rejected conforming source: **§14.2.3 GR8** (`cite.py`-verified) — "If the
  argument is passed by reference, the activated runtime element operates as if the formal parameter occupies the
  same storage area as the argument" — which COBOL.NET realizes through that very image round-trip. Both CALL
  guards (the `ArgText` read half and the `CallStringWrite` write half) now test `IsImageCapable`; the leaf-kind
  wording follows the predicate. Golden `da5_call_comp_group` pins the round-trip IN **and** OUT, since a test that
  only checked "no longer throws" would miss a broken round-trip. A float/COMP-5/INDEX group still stages loud.
- **✅ THE TABLE-SORT HALF IS LANDED TOO (DEVLOG 1106).** `SortEmitter.TableCompare` loud-staged a group key
  containing a BINARY leaf. **§14.9.40.4 GR8** (`cite.py`-verified) is decisive: key data items are "compared
  according to the rules for comparison of operands in a relation condition", and a GROUP operand in a relation is
  class alphanumeric (§8.8.4.2.3 SR2) compared over its representation — which `OperandText` already renders via
  `AsImage()` gated on `IsImageCapable`. So the stale predicate made **SORT and `IF` disagree about the same two
  group operands**: the `IF` compared their byte images while the SORT threw. Golden
  `da5_table_sort_group_key` pins the AGREEMENT (not merely that the SORT runs), including a NEGATIVE key: a
  big-endian two's-complement image is not order-preserving across zero, so a group key holding −1 sorts AFTER one
  holding +1 — **deliberate, GR8-mandated, and the `IF` agrees.** A program wanting value order names the
  ELEMENTARY numeric item as the key. That case exists so a later reader cannot "fix" the byte order and break GR8.
- **⛔ CORRECTION — `MoveEmitter.cs:144` IS NOT A GUARD AND MUST NOT BE MIGRATED.** The first cut of this entry
  listed it among the stale sites; that was wrong. It selects a STRATEGY: when the receiver is not a character
  image it first tries the memberwise leaf-copy fast path (source and receiver leaf layouts positionally
  identical), and only the fall-through reaches the real capability guard at line 168, which is ALREADY on
  `IsImageCapable`. Verified by repro: MOVE into a COMP-containing group works in all three shapes — aligned
  memberwise, non-aligned image redistribution, and from an alphanumeric source. So `MoveEmitter` using both
  predicates is not "a distinction disagreeing with itself"; the two uses have different jobs.
- **✅ THE REMAINING FIVE GROUP-RECEIVER GUARDS ARE LANDED (DEVLOG 1107) — DA5 IS COMPLETE.**
  `StringEmitter.cs:151` (STRING INTO) · `:193` (UNSTRING INTO) · `InspectEmitter.cs:89`
  (INSPECT REPLACING/CONVERTING) · `AcceptDisplayEmitter.cs:66` (ACCEPT into group) · `:129` (ACCEPT temporal).
  **⛔ AND THE PREDICTION IN THE PREVIOUS REVISION OF THIS ENTRY WAS WRONG.** It said these were "not automatically
  bugs" because STRING/UNSTRING/INSPECT are defined over CHARACTER POSITIONS, so refusing a byte-imaged COMP group
  "may well be CORRECT". Measured, that reasoning is misapplied: **a group MOVE into the very same receiver already
  worked**, and all of these are the same POSITIONAL character transfer into the group's storage (§14.9.25.4 GR4 —
  the group move is alphanumeric, "filled without consideration for the individual items"). So five verbs
  disagreed about one receiver. "BYTES ARE NOT TEXT" governs *rendering a COMP leaf's VALUE as text* (that is
  `DisplayTextWidth`'s job, DA2), NOT writing characters positionally over its bytes.
  Golden `da5_group_verbs_comp_leaf` pins the invariant that matters: STRING and UNSTRING land **byte-for-byte what
  the group MOVE lands** (`MOV`/`STR`/`UNS` lines identical), and INSPECT REPLACING leaves the binary leaf's `00 00`
  untouched because those bytes are not spaces. A float/COMP-5/INDEX group still stages loud on all four, with the
  message now naming the predicate actually tested.
- **⏳ ONE SITE REMAINS AND IT IS A DIFFERENT DEFECT — see DA6.** `NumericRenderer.cs:186` stays on
  `IsCharacterImage` DELIBERATELY: migrating the predicate would be the wrong fix, because a group used as a
  NUMERIC operand is ILLEGAL SOURCE, not a representable operation. Tracked separately.
  *(Historical note, kept because the reasoning is instructive:* **⛔ V59's own governing lesson is BYTES ARE NOT
  TEXT:** a COMP leaf's image is
  radix-2 bytes, not its digits, so a verb defined over CHARACTER POSITIONS (STRING/UNSTRING/INSPECT) may be
  CORRECT to refuse it — consuming those bytes as text would be a silent wrong answer, strictly worse than the
  loud stage. Each site needs its own spec derivation (does the operation need TEXT or STORAGE?), plus the
  separate question of whether a syntax-rule violation is being deferred to RUN TIME where it belongs at COMPILE
  time (e.g. §14.9.43.3 SR1 requires STRING's identifiers be usage display or national). **The caution was right in
  KIND and wrong in APPLICATION** — it correctly refused a blind predicate swap, and the per-site derivation it
  demanded is what showed the swap was in fact correct for all five. The half that survives is the staging
  question, now DA7.*)
- **The inventory is PINNED, so nothing can be forgotten.**
  `tests/Cobol.Net.Tests.Unit/V59ImagePredicateDriftTests.cs` asserts the exact per-file set (and that CALL's two
  halves stay in lockstep), so adding, removing, or silently migrating a site fails a test and forces the decision
  to be recorded. It counts CODE only — the first cut failed on its own explanatory comment.

### DA4 · [MAJOR] · inspect-string · ✅ LANDED (DEVLOG 1110) — a function-identifier in every STRING/UNSTRING SENDING position
> **Grammar + binder, four positions, one helper.** §14.9.43.2 / §14.9.48.2 write these operands as identifier-N and
> §8.4.3.1.2 Format 1 makes `function-identifier-1` a FORMAT of an identifier, so all four admit one: STRING
> identifier-1 and identifier-2 (DELIMITED BY), UNSTRING identifier-1 (the source) and identifier-2/-3
> (DELIMITED BY … OR …). `functionCall` is keyword-led so it goes FIRST in each alternative and cannot be shadowed
> by `dataReference`; a keyword-OMITTED function still parses as a dataReference and resolves in the binder as before.
> **⚠ THE `INTO` PHRASES ARE DELIBERATELY NOT OPENED** — §8.4.3.2.3 SR1 (`cite.py`-verified): "A
> function-identifier shall not be specified as a receiving operand." All four changed positions are SENDING.
> **The UNSTRING source needed a bound-tree change and it made the code SIMPLER:** `BoundUnstringStmt.Source` was a
> `Place`, which a function result has none of, and is now a `BoundOperand` like every other sending operand in
> these two statements — the emitter had only ever been wrapping the Place to reach `OperandText.AsString`, THE one
> string-context renderer, so it now passes the operand straight through. SR2's category screen reads whichever
> shape arrived (a field's PICTURE or an intrinsic's §15.2 result category), so `UNSTRING FUNCTION ORD(C)` is still
> correctly refused as category numeric. Golden `da4_function_sending_operand` pins all four positions.

### DA4 (original entry, for provenance) — the STRING statement's sending operand REJECTS a function-identifier at PARSE time
- **Spec:** §14.9.43.2's general format gives the sending operand as **`identifier-1`**, and §8.4.3.1.2 **Format 1
  of an identifier IS `function-identifier-1`** (`cite.py --check`-verified). No syntax rule excludes a function:
  §14.9.43.3 SR1 requires the identifiers be "described implicitly or explicitly as usage display or national",
  which a function's §15.4 temporary elementary item satisfies implicitly, and **SR8 explicitly CONTEMPLATES a
  numeric identifier-1** ("Where identifier-1 or identifier-2 is an elementary numeric data item, it shall be
  described as an integer…"). So `STRING FUNCTION ORD(C) DELIMITED BY SIZE INTO A` is CONFORMING SOURCE.
- **Observed (2026-07-29, while landing DA2):** it does not reach the binder at all —
  `error COBOL0001: no viable alternative at input 'FUNCTION'`. This is a GRAMMAR gap, not an emit gap, which is
  why DA2's fix does not cover it: DA2 was the string-context RENDERER, and this operand never gets that far.
- **Scope note:** discovered by testing whether DA2's root cause reached other string contexts. DISPLAY and
  MOVE-to-alphanumeric did (both fixed by DA2); STRING is rejected one stage earlier. Grammar changes are
  pre-authorized, so this is a `Core/*.g4` sending-operand alternative plus the bind path and a golden.

### DA3 · [MAJOR] · conditions · ✅ LANDED (DEVLOG 1110) — and the ROOT CAUSE was three copies of one dispatch
> ⚠ **THE SWEEP WAS NOT COMPLETE, and "three copies" should not be read as a closed count.** `PB4`
> (DEVLOG 1119) found the same hexadecimal-literal defect in FIVE further positions — the whole VALUE
> family and the `ALL <hex>` figurative — because the prefix-letter list in `CobolLiteral` was itself
> duplicated, and BOTH copies omitted `X`. DA3 fixed the three OPERAND dispatches it could see from the
> reported symptom; the decoder underneath them was the shared cause and was not examined. When a finding
> names N copies of a rule, the count is what the symptom exposed, not what exists.
> §8.3.3.2 **Format 2** is the hexadecimal-alphanumeric FORMAT *of* the alphanumeric literal, and §8.3.3.2.1 makes
> every format of it "of the class and category alphanumeric" (both `cite.py`-verified). So `X"…"` belongs wherever
> an alphanumeric literal belongs.
> **⛔ THE REAL DEFECT WAS DUPLICATION, NOT A MISSING ARM.** The "non-numeric literal → operand" chain existed in
> THREE hand-maintained copies — `ExpressionBinder.LiteralOperand`, `IntrinsicBinder.NonNumericOperand`, and inline
> in `ConditionBinder`'s comparison-operand binder — and the hex form was simply absent from one of them. That is
> why the same literal worked in a MOVE and staged loud as "comparison operand" in a relation. Adding a fourth arm
> to a third copy would have guaranteed the next literal form broke too, so the fix EXTRACTED one canonical
> `NonNumericLiteralOperand` mapping that all three now call; a new literal form is a one-line change in one place.
> Golden `da3_hex_literal_operand` pins all three positions (relation, MOVE, intrinsic argument).

### DA3 (original entry, for provenance) — a HEXADECIMAL literal as a comparison operand is staged loud at RUN TIME
- **Spec:** §8.3.3.2 defines the hexadecimal format of an alphanumeric literal (`X"F0F1"`), and it is an
  ALPHANUMERIC LITERAL — §8.8.4.1.1 admits a literal on either side of a relation condition with no format
  restriction. So `IF G = X"FFF94142"` is CONFORMING SOURCE.
- **Observed (2026-07-29, while re-pinning the V59 group-image test):** the program compiles and then throws
  `NotImplementedCobolFeatureException: comparison operand` at run time. A quoted literal in the same position works
  (`IF G = "000PAB"`), so the gap is the hex FORM reaching the comparison-operand renderer, not the comparison.
  Hex literals DO work in a VALUE/ALPHABET position (DA1 landed that decode), which is what hid this.
- **Scope note:** not V59; the V59 test pins the same fact with an alphanumeric REDEFINES instead and says so.

### DA2 · [MAJOR] · accept-display · ✅ LANDED (DEVLOG 1104) — a NUMERIC FUNCTION operand in ANY string context
> **Landed 2026-07-29.** The gap was WIDER than "DISPLAY": `OperandText.AsString` is the one string-context
> operand renderer, so `MOVE FUNCTION ORD(C) TO` a `PIC X` item failed identically. One arm fixes every such
> context. **What the investigation actually found is bigger than the ticket:** the compile-time FOLD was
> OBSERVABLE — an intrinsic over constant arguments folds to a numeric literal and printed, while a computed one
> threw, so the two paths had different user-visible behaviour. The fix therefore had to pin ONE rendering rule
> for both, not merely stop the throw. §15.4.1 and §14.9.11.4 GR1 both make the choice implementor-defined; the
> determination (literal form — no zero padding, leading `-`) is documented in `docs/CONFORMANCE.md` and pinned by
> the golden `da2_function_as_text`, whose first four lines are two fold/compute pairs of the same value.
> Deliberately differs from GnuCOBOL, which zero-pads a computed `ORD` but prints a folded `MAX` minimally.
- **Spec:** §14.9.11.2 Format 1 (device) takes `identifier-1`; §8.4.3.1.2 gives **`function-identifier-1`** as one of
  the general formats of an identifier; §14.9.11.3 SR1 excludes only class message-tag, object and pointer — nothing
  excludes a function. So `DISPLAY FUNCTION ORD(C)` is CONFORMING SOURCE. All three clauses `cite.py --check`-verified.
- **Observed (2026-07-29, while writing the V59 byte-image golden):** `DISPLAY FUNCTION ORD(C)` compiles and then
  throws `NotImplementedCobolFeatureException: computed expression in a string context` at run time, while
  `MOVE FUNCTION ORD(C) TO N` + `DISPLAY N` works. A COMPILE-TIME-FOLDABLE intrinsic is fine
  (`DISPLAY FUNCTION BYTE-LENGTH(G)` prints), which is what hid it: the gap is a RUNTIME-computed NUMERIC intrinsic
  in a string context.
- **Where:** `OperandText.AsString` intercepts only an intrinsic whose `ResultCategory` is alphanumeric/national/
  boolean; a NUMERIC-result intrinsic falls to the visitor's computed arm, which is the loud stage. The fix is the
  numeric-result arm — render through the numeric channel and format with the intrinsic's own profile — not a new
  mechanism.
- **Scope note:** this is NOT V59 and was not caused by it; the V59 golden routes the ordinal through a numeric item
  and says so in a comment, so the gap stays visible rather than absorbed.

### DA1 · [MAJOR] · special-names · ✅ LANDED (DEVLOG 1019) — verified end-to-end (char-THRU worked, isolating the defect to hex decode) + root-caused + fixed
- **Spec:** ISO §12.3.7 k)5 (ALPHABET literal-1 THRU literal-2 — "the native run from operand-1 to operand-2, either
  direction, ascending positions"); §8.3.3.6.4 GR6/GR7 (HIGH-/LOW-VALUE = the PCS position extremes).
- **Observed:** `ALPHABET AL IS X"FF" THRU X"00"` used as the PROGRAM COLLATING SEQUENCE leaves the collating table at
  the native pins (a run showed HIGH-VALUE = X"FF", LOW-VALUE = X"00") instead of reversing. Per §12.3.7 k)5 the
  descending run X"FF"→X"00" must place X"FF" at position 1 and X"00" at position 256, so HIGH-VALUE = X"00" and
  LOW-VALUE = X"FF". A plain string-literal alphabet (`"ZYX…A"`) reorders correctly (CA3's golden relies on it), so the
  defect is isolated to the `THRU`/hex-operand arm of `DataBinder.AlphabetBind` (`DataBinder.Switches.cs` ~:376-388) —
  `operands[0].Length == 1 && operands[1].Length == 1` guard fails and the range is skipped.
- **VERIFIED + FIXED (DEVLOG 1019):** a char-literal THRU (`"Z" THRU "A"`) reversed correctly, isolating the defect to
  HEX decode. ROOT CAUSE was `DataBinder.LiteralChars` — it decoded quoted-string and integer literals but let a
  HEX-format literal (`X"hh…"`, §8.3.3.2) fall through to raw text (length != 1), so the THRU/ALSO guard skipped the
  range. FIX: route a hex-shaped literal through the existing `CobolLiteral.DecodeHex`. The same decoder serves the
  CLASS clause (fixed too); the national-alphabet path correctly rejects hex (SR14c2). Golden
  `2002/da1_alphabet_hex_thru`.

### SR1 · [MAJOR] · grammar/optional-words · ✅ LANDED (DEVLOG 1041) — the first bug the SPEC RECONCILIATION proved

**Source:** not the code audit. This came out of the PDF-vs-markdown reconciliation, and it is the first case of the
GRAMMAR having inherited a transcription defect rather than the transcription alone being wrong. It is the exit
criterion of REPAIR-PLAN Batch 1.

- **Spec:** ISO §5.2.2 (an underlined uppercase word is REQUIRED) and §5.2.3 (a non-underlined one is an OPTIONAL
  WORD that may be written or omitted, with no change of meaning). In every arithmetic statement's printed general
  format `SIZE`, `ERROR` and `NOT` are underlined and **`ON` is not**.
- **Measured, not assumed:** `scripts/spec/figure_extract.py` reads the underline rectangles per word. `ON` comes
  back plain on p632 (COMPUTE), p644 (DIVIDE), p703 (MULTIPLY), p607 and p756 (ON EXCEPTION); `SIZE`/`ERROR`/`NOT`
  come back underlined on those same pages, so it is not a detection artifact.
- **Observed:** `ADD A TO B SIZE ERROR DISPLAY "OVERFLOW" END-ADD` → `COBOL0001: no viable alternative at input
  'SIZE'`. Legal COBOL rejected.
- **The grammar contradicted itself,** which is what makes this decision-complete rather than a reading of the spec:
  `callOnExceptionPhrase` already had `ON? (EXCEPTION | OVERFLOW)`, while `arithmeticOnSizeError`,
  `computeOnSizeError` and `mcsExceptionPhrases` required the bare token. One file, two answers.
- **FIX (landed):** all three rules take `ON?`. Swept rather than patched at the site — no non-comment rule now
  requires a bare `ON` before `SIZE` or `EXCEPTION`. No edition gate: §5.2.3 is not version-gated.
- **Golden:** `tests/conformance/85/optional_on_size_error` — pins `ON` as omittable across ADD/SUBTRACT/MULTIPLY/
  DIVIDE/COMPUTE, proves each phrase still binds to its own role, and includes a MIXED case (`ON` omitted on one
  phrase, written on the other, in the reversed order `phrase_order_arithmetic` established) so the optional-word
  rule and the §5.2.6.4 choice-indicator rule are shown to COMPOSE.

### SR2 · [MAJOR] · optional-words · ⚠ ROOT CAUSE — "unbracketed" was used as the test for "required word"

**One wrong criterion, five sites.** The codebase repeatedly justifies a "leniency" with the reasoning that a
word is *unbracketed in the ISO format, therefore required*. **That criterion is wrong.** ISO §5.2.2/§5.2.3 make
**underlining** the test for a required word; bracketing marks whether a whole PHRASE may be omitted, not whether
a WORD inside it must be written. The two are independent, and conflating them turns conforming source into a
diagnosed "extension".

**Measured, per word, off the printed pages:**

| word | context | printed | pages |
|---|---|---|---|
| `KEY` | `INVALID KEY` | `INVALID` underlined, `KEY` **not** | 635, 722, 740, 784, 816 — all five |
| `KEY` | `RECORD KEY` clause | `RECORD`/`SOURCE` underlined, `KEY`/`IS` **not** | 359 |
| `COLLATING` | SORT/MERGE | **not** underlined | 687, 776 |
| `AT` | `AT END` | **not** underlined, 0 of 11 occurrences | 600–829 sweep |
| `PRINTING` | `SUPPRESS` | **not** underlined | 795 |

So `INVALID <imperative>`, `RECORD data-name`, `SEQUENCE alphabet-name`, `SEARCH … END …` and bare `SUPPRESS`
are **conforming ISO**, not vendor extensions.

**LANDED (grammar, DEVLOG 1043):** `searchAtEndClause` → `AT?` (it was the lone hold-out — `readAtEnd`,
`returnAtEndPhrase` and `writeAtEndOfPage` already had it, and this rule instead admitted the AT-less form via a
separate alternative *labelled a NIST/IBM extension*, which also silently denied the AT-less spelling to the NOT
branch). `suppressStatement` → `PRINTING?`. The two misclassified-leniency comments in `CobolIO.g4` corrected.
Goldens: `2002/rw_suppress_bare` (byte-identical output to `rw_suppress`, proving omission is a SPELLING).

**REMAINING — legacy only, and it reports CONFORMING SOURCE.** `DialectStrictnessChecks.CheckInvalidKeyNoiseWord`
raises `CBL3611` (error, strict) / `CBL3612` (warning) when `KEY` is omitted, and leniency **L5** does the same
for `COLLATING`. Both fire on legal COBOL. They live in `src/CobolSharp.Compiler` — the differential oracle,
deleted at P15 — and `src/Cobol.Net.*` has no equivalent, so the LIVE compiler is unaffected. Fix or delete with
the P15 cut-over; do not port. `docs/dialect-strictness.md` L1/L5 need the same correction.

**Sweep still owed:** `KEY`, `ON`, `RECORD` and `WITH` are genuinely construct-dependent (measured split across
pages 600–829), so each site needs its own page checked. That is the remaining half of Batch 1's exit criterion.

## ✅ OWNER-DECIDED (2026-07-22 — APPROVED, now fix-ready)

- **CHECKING-OFF DOCTRINE, decided 2026-07-28 — applies to the WHOLE EC super-batch (CA9/CA10/CA11/V55/CA37/CA38),
  not to one finding.** When exception checking is OFF, a fatal-EC raise site is **LENIENT wherever the standard
  names the outcome, and a LOUD ABORT wherever it names none.** Lenient: CA37/CA38 (§14.9.39 GR30 'capacity
  unchanged' / GR31 'SET not executed'), CA9's SET pointer UP/DOWN BY (Format 10 GR19 'unsuccessful … content of
  identifier-9 is unchanged'), CA10's scratch-read, CA11/V55's skip. Loud abort: the four `CobolPtr.Deref` sites —
  `Deref` returns a `StorageCell`, so there is no unchanged state to return and leniency would mean continuing on a
  fabricated cell. §13.18.5.4 GR3/GR4 name no outcome. ⚠ This CORRECTS the CA9 delta as drafted, which keeps the
  loud throw at all six raise sites: `UpBy`/`UpByScaled` become lenient. Justification incl. the five-compiler
  survey (all of GnuCOBOL, gcobol, Micro Focus, IBM Enterprise COBOL and NetCOBOL hard-stop; none continues):
  `DESIGN-ec-oo-superbatch.md` §Risks, first bullet.

- **CA12 CO-LANDS, decided 2026-07-28 — ordered LAST in the EC chain (step 11).** Every fatal-EC finding in the
  batch (CA9/CA10/CA11/V55/CA37/CA38) then inherits the outward-GLOBAL walk instead of each shipping the same
  hole. Deferring it would have been a GAP against P14's zero-GAP definition of done, inherited by six findings.
  The split it closes is visible in the emitted code: the I/O path already walks outward (`ProgramEmitter.cs:280`
  → `return __outer.__RunGlobalUse(__f);   // continue outward (§14.9.49.4 GR4b)`) while the EC dispatch tail
  emits a bare `return -3;` — one spec rule (§14.9.49.4 GR3g, "the search is repeated as specified in General
  rule 4"), two behaviours.

- **V55's method-side "enabled" literal, decided 2026-07-28 — and it CORRECTS the written delta.** The TURN-state
  source exists (`BoundCompilation.Turn`, group-wide and LINE-KEYED; fold at the METHOD-ID header line, since the
  raise is in the `__CobolInvoke` prologue before any method statement runs). ⛔ But it is folded at **BIND TIME,
  not "at emit time" as the delta says** — codegen holds no `TurnState` anywhere; every TURN query lives in the
  binder and codegen consumes only `BoundEcChecked` nodes and `EcState` flags, so an emitter-side read would be a
  second mechanism for the binder's job. Fold in `DataBinder.OoBindMethodData`, record on `OoMethodSymbol`, read
  the plain bool in `OoEmitter`. The not-enabled-in-both path throws a NEW non-attributing
  `CobolImplementorFatalException`: `CobolFatalException`, `CobolCallException` and `CobolSizeError` all carry an
  EC name, and GR7c requires this path to stop without attributing EC-OO-UNIVERSAL.

- **CA14 → ✅ LANDED (DEVLOG 1094) — APPROVED option (a): enforce the uniform introduction-error policy.**
  ⛔ **The premise "the SOLE policy exception" was FALSE.** The introduction-axis theory added with the fix
  (`VersionMatrixTests.IntroducedConstruct_IsRejectedUnderPermissive`) found two more on its first run:
  `receive-as-user-word` and `end-receive-as-user-word` at COBOL-85, where the §8.9 arm hard-coded a
  `Removed` verdict. That arm now COMPUTES the verdict from the word's own reservation interval. As landed:
  replace `DataBinder.cs:2526`
  `Edition.Removed(EditionCodes.Introduction,…)` with the canonical funnel
  `ConstructRegistry.Check(Edition.Edition, Sink, Constructs.SyncOnGroup2023, "data item 'G'")`, activating the
  already-present `sync-on-group-2023` row so SYNC-on-group is a hard error on BOTH axes like every other 2023
  introduction (removes the sole policy exception). Update the now-obsolete owner-disposition comments
  (`constructs.json:1866`, `DataBinder.cs:2521-2530`). Effort S.
- **V59 → SCOPE CORRECTED 2026-07-28 (owner: "every byte boundary"); the recipe below is superseded in its
  MECHANISM, not its intent.** Three things were established before implementing, each verified rather than
  reasoned:
  1. **The representation was ALREADY CHOSEN and documented — the image just ignores it.** `PicInfo.StorageWidth`
     pins BINARY at 1-2-4-8 and PACKED at `Digits/2+1` BCD; `DataItem.ByteWidth` documents them; `FUNCTION
     BYTE-LENGTH` reports them. There is no new representation to invent and a second one must not be invented.
  2. **The compiler contradicts itself, observably, with no file and no byte pun.** For `05 G-COMP PIC 9(4) COMP.
     05 G-PACK PIC 9(4) COMP-3.` it answers `BYTE-LENGTH(G) = 5` and `LENGTH(G) = 8`, and accepts `REDEFINES G PIC
     X(8)`. §15.14.4 r1 (bytes) and §15.50.4 r3 (alphanumeric character positions) cannot disagree in a
     single-byte-character model. **This makes V59 a genuine conformance defect, not only implementor latitude** —
     the original adjudication reached "not a clear §4.2.16 violation" by weighing only the byte-pun view.
  3. **`RedefCodec`/Tier C is NOT the mechanism.** The whole-group image is a Latin-1 `string`, and files, SORT and
     the Tier-B REDEFINES backing all consume that one image. Making a BINARY/PACKED leaf's image its true bytes
     (Latin-1-carried) fixes every boundary at once through the mechanism that already exists. Tier C's separate
     `byte[]` canonical stays unrealized and unneeded — its reject list (float/COMP-5/INDEX) is unchanged.
  **Effort: L, and larger than stated** — the leaf's image WIDTH changes from `Pic.Digits` to `StorageWidth`, and
  `ImageWidth` has 129 references across 30 files (§14.4 itself flagged this axis: "would change `ImageWidth` and
  every offset computation"). Measured blast radius in the corpus: **zero** — no conformance golden and no NIST
  program has a COMP/COMP-3 field inside an FD record, which is also why the divergence was never caught.
  Empirically today a `PIC 9(4) COMP` and a `PIC 9(4) COMP-3` both reach a sequential file as ASCII `31 32 33 34`.
  ⛔ The `ComputeTier` reject-list still stays untouched. Design SSOT annotated at `COBOLNET_DESIGN.md` §14.4.

- **V59 → superseded APPROVED option (B): build the Tier-C byte[] canonical** (current value-faithful Tier-B zoned image is
  ACCEPTABLE INTERIM). Route a REDEFINES/RENAMES class mixing a BINARY/PACKED leaf with a differently-represented view
  to Tier C: a `byte[]` canonical via `RedefCodec` `GetBinary/PutBinary` (radix-2, width+endian) + `GetPacked/PutPacked`
  (BCD nibbles + sign), so a character view reads the leaf's TRUE bytes (matches GnuCOBOL + the §13.18.60.4 GR4/GR11
  letter + the real-program byte-pun fidelity mission). Do NOT add `Usage.Binary`/`Usage.Packed` to the zoned-image
  branch (`ComputeTier` reject-list stays). Effort L (the RedefCodec). Interim: the current zoned image stands until it
  lands (so this is NOT a blocker to the rest of the queue).

## (Original owner-decision adjudications — retained for reference)

### CA14 [editions-gating] — §E.3.2 item 6; §13.18.55 (SYNCHRONIZED clause); §4.2.9 Standard extensions; §4.2.2 Acceptance of standard language elements (the warning mechanism)
> ⛔ **CITATIONS CORRECTED WHEN THIS LANDED (2026-07-28).** Three of this entry's were wrong — the inherited-citation
> failure CLAUDE.md rule 1 names. 'standard extension' is **§4.2.9**, not §4.2; the warning mechanism is **§4.2.2**,
> not §4.2.1; and 'line 48926' is an EMPTY LINE even in the spec revision it was written against (E.3.2 item 6 was at
> 49438 there). The adjudication below is otherwise unchanged, and its reasoning is unaffected — but read its line
> numbers as the artefacts they are. Every clause above is `cite.py --check`-verified.
Independently verified. The DATE is correct and undisputed: SYNCHRONIZED-on-group is a 2023 introduction (§E.3.2 item 6: 'This clause may now be specified for a group level data item'). CODE: DataBinder.cs:2526-2530 gates it via Edition.Removed(EditionCodes.Introduction,...). Edition.Removed (EditionContext.cs:95-99) = Error when strict, Warning when Permissive. So --std 2014 strict correctly REJECTS (COBOLNET0900); --std 2014 --permissive accepts with only a warning. The finding claims this must be a hard error on BOTH axes. Adjudication on the SPEC axis: ISO does not define a '--permissive' mode. §4.2.9 Standard extensions expressly permits an implementor to support additional syntax as a 'standard extension' provided the functionality matches the standard — SYNCHRONIZED-on-group is a spec-correct no-op in the typed-native model, so accepting-it-with-a-warning under a non-standard permissive mode is defensible as a standard extension, NOT an ISO violation; §4.2.2 requires only that a warning mechanism EXIST, which --permissive satisfies. The strict/default axis (the spec-governed one) is already correct. Therefore this is NOT a clear ISO-conformance bug. What the finding legitimately exposes is an INTERNAL-CONSISTENCY defect: this is the ONLY site routing an Introduction code (COBOLNET0900) through the Removed severity seam, contradicting the compiler's own documented single-policy contract — EditionContext.Permissive doc (:53 'Introduction gating ... is an error on BOTH axes'), EditionSeverityPolicy.For(NotYetIntroduced)=>Error on both (:25), and the dormant registry row sync-on-group-2023 (ConstructRegistry.g.cs:164, introducedIn=2023) which, if routed through ConstructRegistry.Check, would itself yield Error on both axes. HOWEVER the deviation is an EXPLICIT, documented OWNER disposition: constructs.json:1866 ('GATED P3 step 10 (owner-chosen disposition)', 'ACCEPTED-INERT under --permissive ... keeps INV-1 continuity') and the DataBinder.cs:2521-2530 comment. Because ISO does not compel rejection under a non-standard permissive extension mode AND the owner explicitly chose accept-inert here, this is an owner decision, not a defect to confirm or refute unilaterally. CHOICES: (a) Enforce the uniform policy — replace DataBinder.cs:2526 Edition.Removed(EditionCodes.Introduction,...) with the canonical funnel ConstructRegistry.Check(Edition.Edition, Sink, Constructs.SyncOnGroup2023, "data item 'G'"), activating the already-present sync-on-group-2023 row so it is a hard error on both axes like every other introduction (removes the sole policy exception; recommended, since migration-mode leniency exists for REMOVED features that have pre-removal semantics to preserve, not for a FUTURE feature). (b) Keep the owner's accept-inert disposition but amend the contract docs (EditionContext.Permissive summary, EditionSeverityPolicy doc, ConstructRegistry doc) to record this as a SANCTIONED exception so the 'introductions error on both axes' statement is no longer globally contradicted. Recommend (a).

### V59 [picture-usage-value] — ISO §13.18.60.4 GR4 (USAGE BINARY 'radix of 2'; representation implementor-defined) + GR11 (PACKED-DECIMAL 'radix of 10 ... minimum possible configuration'; representation implementor-defined) + GR2 ('USAGE does not affect the USE of the data item')
The code (DataBinder.ClassifyRedefinesClasses:2795-2814) represents a fixed-point BINARY/PACKED leaf of a Tier-B (StringCanonical) REDEFINES class as a Pic.Digits-wide zoned DISPLAY image (MarkImageForced + SignKind→ImageSignKind); ComputeTier (:2861-2867) routes plain Usage.Binary/Usage.Packed to Tier B (only float/COMP-5/BINARY-CHAR..DOUBLE/INDEX are loud-rejected to the unimplemented Tier C). I read the decisive spec text. GR4 (ISO_COBOL.md:22505): 'The USAGE BINARY clause specifies that a radix of 2 is used to represent a numeric item ... Each implementor specifies the precise effect ... upon the alignment and representation of the data item ..., including the representation of any algebraic sign.' GR11 (:22527): PACKED 'radix of 10 ... each digit position shall occupy the minimum possible configuration ... Each implementor specifies the precise effect ... upon ... representation ...'. GR2 (:22493): 'The USAGE clause ... does not affect the USE of the data item.' KEY: the byte-level representation of BINARY/PACKED is EXPLICITLY implementor-defined, and the value round-trips faithfully through the zoned image (a truncation-disciplined COMP/PACKED holds only values in its PICTURE decimal-digit range, which a Digits-wide image can represent exactly — that is why the design accepts them at Tier B but rejects COMP-5/BINARY-* whose ranges exceed the digit count). Consequently EVERY standard-DEFINED operation (arithmetic, comparison, DISPLAY, value-preserving MOVE) yields the correct result; the divergence is observable ONLY by punning the raw bytes (REDEFINES-as-X, group move to alphanumeric, file record image), and for a COMP/PACKED item those bytes are implementor-defined — no CONFORMING program can detect the radix by standard-defined means. So this is NOT a clear §4.2.16 conformance violation; it is textbook implementor-defined latitude. COUNTERWEIGHTS the owner must weigh: (1) GR4 states radix 'IS' 2 and GR11 states 'minimum possible configuration' as normative 'shall' text, and a full-byte-per-digit decimal image honors neither — even if not program-observable, the implementation fails those storage 'shall's; (2) it diverges from GnuCOBOL/IBM/MF (the finding's noted differential), where a COMP field punned as X yields radix-2 bytes — legacy binary-key/bit-manipulation programs that rely on that break; the project's mission (real-program fidelity + GnuCOBOL differential) weighs this heavily. CHOICES: (A) keep the value-faithful Tier-B zoned image (conformant by GR4/GR11 representation latitude, simpler, already value-correct) and document the byte-pun divergence; or (B) implement the interim-rejected Tier-C byte[] canonical (RedefCodec GetBinary/PutBinary + GetPacked/PutPacked preserving radix-2 / BCD) — matches GnuCOBOL and the GR4/GR11 letter, unblocks real binary-punning programs. Because the spec permits the current behavior, this is an owner/architecture call, not a mandated conformance fix; my recommendation given the differential-fidelity doctrine is (B) with (A) acceptable-and-documented in the interim, and the ComputeTier reject-list left untouched (do NOT add Binary/Packed to the zoned-image branch).

## CONFIRMED — verified fix-ready (severity order)

### CA31 · [BLOCKER/M] · perform-flow · ✅ LANDED (DEVLOG 995)
- **Spec:** §14.9.14.4 GR5a (EXIT PERFORM without CYCLE); §14.9.28.4 GR13e (multi-level PERFORM VARYING, TEST BEFORE cascade)
- **Verified:** SPEC: §14.9.14.4 GR5a — 'The execution of an EXIT PERFORM statement without the CYCLE phrase causes control to be passed to an implicit CONTINUE statement immediately FOLLOWING the END-PERFORM phrase that matches the most closely preceding, and as yet unterminated, inline PERFORM statement.' The whole inline PERFORM (all AFTER levels) is left. CODE: A multi-level PERFORM VARYING is emitted as NESTED C# while loops — ControlFlowEmitter.cs:262-310 EmitVarying/EmitBefore recurses one `while (!(cond))` per level, body() only in the innermost (k==levels.Count-1), and each outer level emits `AugmentSetTarget(level)` + `InitVaryingTarget(inner)` AFTER the inner loop closes (lines 277/285-286). BoundExitPerform in the ordinary region (StatementEmitter.cs:129-138, `_ =>` arm) emits a bare `break;` (n.Cycle false). In C# `break` exits ONLY the innermost `while`; control then resumes in the enclosing AFTER-level loop, which runs its augment + reinit and re-tests — so the ENTIRE PERFORM is NOT left and extra iterations execute. Independently confirmed: EmitVarying emits no outer wrapper/label around the nested whiles, so nothing catches the `break` at the whole-PERFORM level. Divergence is real for any inline PERFORM VARYING with >=1 AFTER phrase. (Single-level VARYING and non-VARYING inline forms are accidentally correct because break exits their single loop.)
- **Fix:** ROOT CAUSE: StatementEmitter.cs:137 emits `break;` which only exits one C# loop level, but a multi-level VARYING is nested loops (ControlFlowEmitter.cs:262-310). FIX (one shared change with CA32) — replace break/continue for the ordinary inline PERFORM with goto to labels bracketing the loop, mirroring the existing F3 label machinery. (1) EmitterState.cs:25 — add `Inline` to `internal enum F3Region { None, Imp1, Handler, Finally, Inline }`. (2) StatementEmitter.cs:129-138 — add arm before the `_` fallback: `F3Region.Inline => Emit(n.Cycle ? $"goto __pcont{_dispatchState.F3Cur.Id};" : $"goto __pexit{_dispatchState.F3Cur.Id};", terminated: true)` (keep the `_ => break/continue` as a defensive fallback; the binder only permits EXIT PERFORM inside an inline/F3 PERFORM per §14.9.14.4 SR8, so None is never reached for a valid bind). (3) ControlFlowEmitter.cs: rename the current EmitPerform switch body to `EmitPerformLoop(control, body, inline)` (unchanged); add a wrapper `EmitPerform(control, body, inline)` that for `inline:false` just calls EmitPerformLoop, and for `inline:true` does: `int pid = ctx.Names.NextLoop(); var saved = dispatch.SetF3Region(F3Region.Inline, pid); EmitPerformLoop(control, () => { body(); w.Line($"__pcont{pid}: ;"); }, true); dispatch.RestoreF3Region(saved); w.Line($"__pexit{pid}: ;");`. (4) EmitInlinePerform (ControlFlowEmitter.cs:58-63) — drop the now-redundant `SetF3Region(None)` reset: `EmitPerform(p.Control, () => Statements.EmitStatementList(p.Body), inline:true)` (a nested inline PERFORM now sets its OWN Inline(pid), so EXIT PERFORM in it correctly targets the inner loop — GR5a 'most closely preceding'). `__pexit{pid}` sits AFTER the outermost while, so `goto __pexit` jumps out of every nested AFTER level = leaves the whole PERFORM. Unreferenced labels are safe: the generated file already emits `#pragma warning disable CS0164` (ProgramEmitter.cs:67). MECHANISM: singular pattern — reuses F3Region/SetF3Region; no second dispatch mechanism. NOTE: changes generated C# for every inline PERFORM (break→goto + 2 labels) → regenerate the 32 characterization snapshots.
- **Golden (spec-derived):** Minimal program (COBOL-2002/2014/2023; default --std COBOL-2023):
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CA31.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9.
       01 B PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM VARYING A FROM 1 BY 1 UNTIL A > 3
                     AFTER B FROM 1 BY 1 UNTIL B > 3
               IF A = 2 AND B = 2
                   EXIT PERFORM
               END-IF
               DISPLAY A B
           END-PERFORM
           DISPLAY "DONE"
           STOP RUN.
SPEC-DERIVED EXPECTED OUTPUT (one DISPLAY per line; DISPLAY A B concatenates the two PIC 9 operands per §14.9.13):
11
12
13
21
DONE
Derivation: §14.9.28.4 GR13e drives A=1 with B=1,2,3 (rows 11,12,13); inner exhausts, A augments to 2 and B reinits to 1 (row 21); at A=2,B=2 the IF fires EXIT PERFORM which per §14.9.14.4 GR5a transfers control to the implicit CONTINUE FOLLOWING END-PERFORM — the whole PERFORM is left (the '22' DISPLAY never runs and A never reaches 3). Then DONE. BUGGY ACTUAL (verified by audit against generated pnest.g.cs): 11 12 13 21 31 32 33 DONE — the inner `break` lets A advance to 3 and the A=3 pass runs to completion.

### CA32 · [BLOCKER/M] · perform-flow · ✅ LANDED (DEVLOG 995 — the shared CA31 fix)
- **Spec:** §14.9.14.4 GR5b (EXIT PERFORM CYCLE); §14.9.28.4 GR13d (single-level PERFORM VARYING, TEST BEFORE augment-then-retest)
- **Verified:** SPEC: §14.9.14.4 GR5b — 'The execution of an EXIT PERFORM statement with the CYCLE phrase causes control to be passed to an implicit CONTINUE statement immediately PRECEDING the END-PERFORM phrase that matches the most closely preceding, and as yet unterminated, inline PERFORM statement.' Reaching the point just before END-PERFORM means the loop control still runs; for VARYING, §14.9.28.4 GR13d requires 'the induction variable is incremented by the augment value, and condition-1 is evaluated again.' CODE: For PERFORM VARYING the induction augment `AugmentSetTarget(...)` is emitted as a plain statement at the END of the `while` body (ControlFlowEmitter.cs:277 TEST BEFORE; :305-306 TEST AFTER emits `if(cond) break;` + augment after body()). BoundExitPerform CYCLE (StatementEmitter.cs:137, `_ =>` arm with n.Cycle true) emits a bare C# `continue;`, which jumps to the `while` condition re-evaluation and SKIPS the trailing augment (TEST AFTER: skips both the exit test AND the augment). The induction variable never advances → the loop cannot terminate = infinite loop. Independently confirmed against the emitter structure: EmitVarying places the augment inside the loop body after body(), and C# `continue` bypasses any code between it and the loop-condition check. (PerformTimes uses a `for` whose `i++` runs on `continue`, and UNTIL/FOREVER have no augment, so those inline forms are accidentally correct — VARYING is the broken form.)
- **Fix:** ROOT CAUSE: StatementEmitter.cs:137 emits `continue;` for CYCLE, but the VARYING augment sits at the END of the loop body (ControlFlowEmitter.cs:277/305-306) and `continue` skips it. FIX: the SAME shared change as CA31 — the new `F3Region.Inline` arm emits `goto __pcont{pid};` for CYCLE, and `__pcont{pid}: ;` is emitted at the END of the body but BEFORE the loop-control augment (the wrapper `() => { body(); w.Line($"__pcont{pid}: ;"); }` in EmitPerform, so for a VARYING the label lands between body() and `AugmentSetTarget`). `goto __pcont` therefore falls through to the augment + re-test (TEST BEFORE GR13d) or the exit-test + augment (TEST AFTER GR13c) — loop control runs, so the variable advances and the loop terminates. Files/steps identical to CA31 (EmitterState.cs:25 enum add; StatementEmitter.cs:129-138 Inline arm; ControlFlowEmitter.cs EmitPerform wrapper + EmitInlinePerform simplification). No separate change needed — one fix closes both CA31 and CA32. MECHANISM: label at the loop-control boundary expresses GR5b's 'implicit CONTINUE preceding END-PERFORM' exactly, which a raw `continue` cannot when the augment is emitted as an explicit trailing statement rather than a `for`-increment.
- **Golden (spec-derived):** Minimal program (COBOL-2002/2014/2023; default --std COBOL-2023):
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CA32.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
               IF I = 2
                   EXIT PERFORM CYCLE
               END-IF
               DISPLAY I
           END-PERFORM
           DISPLAY "DONE"
           STOP RUN.
SPEC-DERIVED EXPECTED OUTPUT (one DISPLAY per line):
1
3
DONE
Derivation: §14.9.28.4 GR13d (TEST BEFORE, single level) — I=1: cond 1>3 false, body runs → DISPLAY '1', augment I→2, retest. I=2: cond false, body runs, IF fires EXIT PERFORM CYCLE → per §14.9.14.4 GR5b control passes to the implicit CONTINUE PRECEDING END-PERFORM, so loop control still runs → augment I→3, retest. I=3: cond false, body runs → DISPLAY '3', augment I→4, retest. I=4: cond 4>3 true → leave. Then DONE. BUGGY ACTUAL (verified by audit against generated pcyc.g.cs): the `continue` skips `I = I+1`, so I stays 2 forever — INFINITE LOOP (program never terminates).

### CA1 · [MAJOR/S] · accept-display-misc · ✅ LANDED (DEVLOG 996)
- **Spec:** §14.9.20.4 GR7 (INITIALIZE) — "When a dynamic-length elementary item is initialized, its length is set to zero." (spec line 27679); §8.5.1.10.4 / §13.18.19.4 GR1 (dynamic-length store, minimum length zero)
- **Verified:** SPEC: INITIALIZE GR7 unconditionally sets a dynamic-length elementary item's length to zero. CODE: a dynamic-length PIC X item is category Alphanumeric, so InitializeItemCategory (InitializeBinder.cs:221) returns InitializeCategory.Alphanumeric; for a bare INITIALIZE (no VALUE/REPLACING) InitializeSender (line 200-206) returns BoundFigurative('S'); this InitializeStore is emitted through the ONE MOVE path — InitializeEmitter.cs:39 move.Emit → MoveEmitter.ConvertSource IsDynamicLength branch (MoveEmitter.cs:285-286) → RuntimeApi.DynStore(AsString(SPACE)=" ", limit) → CobolDynString.Store(" ", limit) which returns a 1-char string (value.Length 1 not > limit). Result: the item is left at LENGTH 1 (a single space), never length 0. Verified end-to-end; the DYNAMIC LENGTH surface and FUNCTION LENGTH are live (tests/conformance/2014/dynamic_length_item.cob, set_size.cob).
- **Fix:** E:\CobolSharp\src\Cobol.Net.Compiler\Binding\Procedure\Verbs\InitializeBinder.cs, ExpandInitialize elementary arm (line 148-149). After `if (InitializeSender(cat, item.RawValue, spec) is not { } source) return;` (this preserves GR5c qualification — a dynamic-length item that does NOT qualify, e.g. REPLACING NUMERIC only, stays unchanged), insert: `if (item.IsDynamicLength) source = new BoundStringLiteral("");   // §14.9.20.4 GR7 — a dynamic-length item is initialized to length zero (overrides the GR6c figurative fill)`. Mechanism: an empty BoundStringLiteral flows through the SAME dynamic-length store path — ConvertSource (MoveEmitter.cs:285-286) → DynStore("", limit) → CobolDynString.Store("", limit) returns "" (length 0), exactly as the ConvertSource comment at line 280-281 already documents for a zero-length literal. No runtime change needed (CobolDynString.Store already yields length 0 for an empty sender). DataItem.IsDynamicLength (DataItem.cs:253) is already set on the receiver.
- **Golden (spec-derived):** Edition COBOL-2014+ (default 2023). Program:
 IDENTIFICATION DIVISION.
 PROGRAM-ID. INITDYN.
 DATA DIVISION.
 WORKING-STORAGE SECTION.
 01 WS-D PIC X DYNAMIC LENGTH LIMIT IS 30.
 01 WS-N PIC 9(2).
 PROCEDURE DIVISION.
     MOVE "HELLO" TO WS-D.
     INITIALIZE WS-D.
     MOVE FUNCTION LENGTH(WS-D) TO WS-N.
     DISPLAY "LEN=" WS-N.
     STOP RUN.
SPEC-DERIVED EXPECTED (§14.9.20.4 GR7 — INITIALIZE sets WS-D's length to zero): LEN=00.
CODE PRODUCES (buggy): LEN=01 (INITIALIZE ran an implicit MOVE SPACE, leaving a 1-char single-space item).

### CA10 · [MAJOR/L] · exceptions-ec · ✅ LANDED (DEVLOG 1087)
- **Spec:** §8.4.2.3.4 GR2 (spec:6655); §13.18.38 (ODO); Table 13 (EC-BOUND-SUBSCRIPT/EC-BOUND-ODO = Fatal, spec:24315/24320); §14.6.13.1.3 #5/#7 (spec:24216/24222)
- **Verified:** SPEC: §8.4.2.3.4 GR2 — a subscript <1 or > highest permissible occurrence 'sets EC-BOUND-SUBSCRIPT to exist' (Table 13: Fatal). §13.18.38/GR7-8 the ODO bound. Under checking ON a Fatal EC must run an applicable USE/WHEN (§14.6.13.1.3 #5) or terminate (#7). CODE: CobolTable.At (CobolTable.cs:22-30) returns a per-type Scratch<T>.Slot for occurrence<1 || occurrence>Length — no raise, no throw, no dispatch; OdoExtent (:50-54) silently clamps count to [0,max]. Grep confirms there is NO EC-BOUND-SUBSCRIPT/EC-BOUND-ODO raise site anywhere in the runtime, and EcBinder.EcWrap has no subscript/ODO case (the ambient gates at EcBinder.cs:373-409 list ref-mod, float, perform-varying, etc. but not these). So `>>TURN EC-BOUND-SUBSCRIPT CHECKING ON` is a total no-op: a mandatory Fatal condition is neither raised, dispatched, nor terminated on — the access returns/absorbs a scratch element and execution continues. The checking-OFF scratch behavior is the correct COBOL-85 lenient default; the divergence is entirely the checking-ON path. Broadest gap of the group (both the handler-present AND no-handler cases are wrong) and the most-used construct.
- **Fix:** Mirror the existing fatal-ambient-gate pattern (EC-BOUND-REF-MOD). (1) ExceptionState.cs (next to RefModError:232): add `BoundSubscriptChecking`/`BoundOdoChecking` bool flags + `SubscriptError(detail)`/`OdoError(detail)` helpers that, when the flag is set, `Set("EC-BOUND-SUBSCRIPT"/"EC-BOUND-ODO", fatal:true)` then `throw new CobolFatalException(...)`. (2) CobolTable.cs:22-30 (At): when `table is null || occurrence<1 || occurrence>table.Length` AND `ExceptionState.BoundSubscriptChecking`, call ExceptionState.SubscriptError before returning the scratch slot; leave the scratch return as the checking-OFF lenient default. CobolTable.cs:50-54 (OdoExtent): when `count<integer1 || count>max` AND BoundOdoChecking, call OdoError; else clamp as now. (3) EcEmitter.cs FatalAmbientGates (:109-116): add `("EC-BOUND-SUBSCRIPT","BoundSubscriptChecking")` and `("EC-BOUND-ODO","BoundOdoChecking")` — the statement guard's set/reset + `catch(CobolFatalException) when(nameTest)` then routes the throw to __EcPerform/__EcDispatch (RESUME-capable), else re-throws to terminate. (4) EcBinder.cs EcWrap ambient block (:388-409): add conservative whole-statement gates `if (ctx.EcState.Turn.Enabled("EC-BOUND-SUBSCRIPT", null, line)) enabled.Add(("EC-BOUND-SUBSCRIPT", null));` and same for EC-BOUND-ODO (mirroring EC-BOUND-REF-MOD at :395). Ship an OCCURS conformance test in the same commit.
- **Golden (spec-derived):** IDENTIFICATION DIVISION. PROGRAM-ID. CA10. DATA DIVISION. WORKING-STORAGE SECTION. 01 T. 05 E PIC X(3) OCCURS 5. PROCEDURE DIVISION. DECLARATIVES. H SECTION. USE AFTER EXCEPTION CONDITION EC-BOUND-SUBSCRIPT. DISPLAY "HANDLED". END DECLARATIVES. M SECTION. >>TURN EC-BOUND-SUBSCRIPT CHECKING ON  DISPLAY E(9)  DISPLAY "AFTER". STOP RUN.  — Edition COBOL-2023 (default). Spec-DERIVED: subscript 9 > highest permissible occurrence 5 ⇒ §8.4.2.3.4 GR2 sets EC-BOUND-SUBSCRIPT to exist; Table 13 = Fatal; checking is enabled and an applicable USE names EC-BOUND-SUBSCRIPT ⇒ §14.6.13.1.3 #5 runs the declarative (prints 'HANDLED'), and because the declarative completes normally with no RESUME the run unit is terminated abnormally (#5 last sentence). EXPECTED: stdout 'HANDLED' only, then abnormal-termination diagnostic on stderr, nonzero exit; 'AFTER' is NOT printed. ACTUAL (code): no EC raised, E(9) reads the scratch slot (spaces), 'AFTER' prints, exit 0 — no 'HANDLED', no termination.

### CA11 · [MAJOR/M] · exceptions-ec · ✅ LANDED (DEVLOG 1088)
- **Spec:** §14.9.23.4 GR5 (EC-OO-NULL, spec:28183) / GR7b (EC-OO-METHOD, spec:28205) / GR7g (spec:28223); Table 13 (both Fatal); §14.6.13.1.3 #5
- **Verified:** SPEC: §14.9.23.4 GR5 — 'If identifier-1 is null, the EC-OO-NULL exception condition is set to exist'; GR7b — method not found 'sets EC-OO-METHOD to exist'; GR7g — 'any exception processing statements associated with that exception condition are executed. Execution then proceeds as defined in 14.6.13'. Both Fatal (Table 13), so §14.6.13.1.3 #5 runs an applicable USE under checking ON. CODE: CobolObject.cs RequireNonNull (:48-50) and __CobolInvoke default arm (:35-38) `throw new CobolFatalException("EC-OO-NULL"/"EC-OO-METHOD", ...)` unconditionally. The INVOKE statement (BoundInvoke/BoundInvokeUniversal) is not wrapped — EcBinder.EcWrap QueryFor (:326-372) has no INVOKE case and FatalAmbientGates omit these names — so no catch exists and the throw reaches RunMain:107 → AbnormalTermination. The matching USE AFTER EXCEPTION CONDITION EC-OO-NULL/-METHOD declarative never runs and RESUME is impossible. Same architectural defect as the queued V55 but for the DISTINCT names EC-OO-NULL and EC-OO-METHOD on the RequireNonNull/__CobolInvoke path (V55 covers only EC-OO-UNIVERSAL in OoEmitter.cs).
- **Fix:** (1) EcBinder.cs EcWrap QueryFor switch (:326-372): add `case BoundInvoke or BoundInvokeUniversal: Query(["EC-OO-NULL", "EC-OO-METHOD"]); break;` (precise per-statement gate, like BoundCallProgram at :354). (2) EcEmitter.cs FatalAmbientGates (:109-116): add `("EC-OO-NULL","OoNullChecking")` and `("EC-OO-METHOD","OoMethodChecking")`. (3) ExceptionState.cs: add the `OoNullChecking`/`OoMethodChecking` flags; optionally have RequireNonNull/__CobolInvoke `Set(name, fatal:true)` before the throw when the flag is on (so the last-exception status is captured) — the throw already carries the correct EC name, so the statement guard's `catch(CobolFatalException) when(nameTest)` will now match and route to __EcPerform/__EcDispatch (RESUME-capable); checking-OFF keeps today's loud abort (#8). Ship an INVOKE-null conformance test.
- **Golden (spec-derived):** IDENTIFICATION DIVISION. PROGRAM-ID. CA11. DATA DIVISION. WORKING-STORAGE SECTION. 01 OBJ USAGE OBJECT REFERENCE. PROCEDURE DIVISION. DECLARATIVES. H SECTION. USE AFTER EXCEPTION CONDITION EC-OO-NULL. DISPLAY "HANDLED". RESUME AT KEEP-GOING. END DECLARATIVES. M SECTION. >>TURN EC-OO-NULL CHECKING ON  INVOKE OBJ "M"  . KEEP-GOING. DISPLAY "OK". STOP RUN.  — Edition COBOL-2023. Spec-DERIVED: OBJ is NULL by default; INVOKE with identifier-1 null ⇒ §14.9.23.4 GR5 sets EC-OO-NULL to exist (Fatal) before any method resolution. Checking enabled + applicable USE ⇒ §14.6.13.1.3 #5 runs the declarative ('HANDLED'); RESUME AT KEEP-GOING (§14.9.33.4 GR3) ⇒ 'OK'. EXPECTED: 'HANDLED' then 'OK', exit 0. ACTUAL (code): CobolObject.RequireNonNull throws unconditionally, no wrapper catches it, RunMain aborts — neither line printed, nonzero exit.

### CA11 · [MAJOR/L] · oo · ✅ LANDED (DEVLOG 1088) — the OO INVOKE conditions (EC-OO-NULL / EC-OO-METHOD)
- **Spec:** §14.9.23.4 GR5 (EC-OO-NULL, line 28183) / GR7b (EC-OO-METHOD, line 28205); §14.6.13.1.3 #4/#5 (lines 24212/24216)
- **Verified:** NOTE: cross-listed — CA11's audit heading is EXCEPTIONS & CHECKING, but its subject/construct is INVOKE + EC-OO-NULL/EC-OO-METHOD (OO), and it is the direct sibling of the OO-assigned V55, so I verified it. SPEC: §14.9.23.4 GR5 — a null identifier-1 sets EC-OO-NULL to exist; GR7b — an unresolved method sets EC-OO-METHOD to exist (both Table-13 FATAL). §14.6.13.1.3: when a fatal EC exists, #4 (WHEN phrase of an exception-checking PERFORM) and #5 ('If checking for the exception condition is enabled and there is an applicable USE statement ... the associated declarative is executed') require the handler to run; the user may RESUME (NOTE 1/2). CODE: CobolObject.RequireNonNull (CobolObject.cs:48-50) and the __CobolInvoke default arm (CobolObject.cs:35-38) `throw new CobolFatalException('EC-OO-NULL'/'EC-OO-METHOD', ...)` UNCONDITIONALLY. The INVOKE statement is NEVER wrapped for these names — EcBinder.EcWrap's QueryFor switch (EcBinder.cs:326-372) has NO BoundInvoke/BoundInvokeUniversal case, and EC-OO-* is in no ambient gate — and EcEmitter.FatalAmbientGates (EcEmitter.cs:109-116) lists only EC-ARGUMENT-FUNCTION/-BOUND-REF-MOD/-DATA-NOT-FINITE/-DATA-OVERFLOW/-RANGE-PERFORM-VARYING, none EC-OO. So no BoundEcChecked wrapper and no `catch (CobolFatalException) when (EcName==...)` ever covers an INVOKE. The throw therefore propagates uncaught to ProgramTable.RunMain:107 `catch (CobolFatalException fx) { AbnormalTermination(...) }`, which only prints to stderr + sets exit 1 — NO __EcDispatch/__RunUse. Under `>>TURN EC-OO-NULL CHECKING ON` with an applicable USE AFTER EXCEPTION CONDITION EC-OO-NULL declarative, the declarative is never selected and RESUME is impossible. (The checking-OFF case and the checking-ON-no-handler case are defensible under §14.6.13.1.3 #8/#7; the confirmed divergence is checking-ON WITH an applicable handler.)
- **Fix:** Three-part, architectural (mirrors the existing FatalAmbientGates dispatch pattern). (1) src/Cobol.Net.Compiler/Binding/Procedure/Verbs/EcBinder.cs — add `private static readonly string[] OoInvokeNames = ["EC-OO-NULL","EC-OO-METHOD"];` and, in QueryFor's switch (near :354), `case BoundInvoke or BoundInvokeUniversal: Query(OoInvokeNames); break;` so an INVOKE under `>>TURN EC-OO-* CHECKING ON` binds a BoundEcChecked. (2) src/Cobol.Net.Compiler/CodeGen/EcEmitter.cs — give the INVOKE its EC catch: either add the two names to a new OO fatal-gate list consumed the same way as FatalAmbientGates (emit `try { EmitStatement(inner) } catch (CobolFatalException __oo when (__oo.EcName=="EC-OO-NULL"||__oo.EcName=="EC-OO-METHOD")) { int __r = EcDispatchExpr(__oo.EcName, ""); if (__r>=0) { __pc=__r; break; } if (__r != -2) throw; }`), so a matching USE declarative / F3 WHEN runs and RESUME AT works. (3) src/Cobol.Net.Runtime/Control/CobolObject.cs — the throws already carry the correct EcName, so they need no change for the handler-present case; the runtime already terminates correctly for checking-off/no-handler because the catch re-throws (`if (__r != -2) throw`). Root architectural note: CA9/CA10/CA12/CA22 are the same 'runtime fatal EC never routed through the emitter's dispatch scaffold' class — a shared design fix (an OO-fatal gate + INVOKE EcWrap case) resolves CA11 and V55 together.
- **Golden (spec-derived):** EC-OO-NULL: `>>TURN EC-OO-NULL CHECKING ON` · CLASS-ID C with instance METHOD-ID M (DISPLAY 'M-RAN'). · Main: 01 OBJ USAGE OBJECT REFERENCE C (default NULL, §13.18.63). · DECLARATIVES: `USE AFTER EXCEPTION CONDITION EC-OO-NULL` → DISPLAY 'HANDLED' then RESUME AT KEEP-GOING. · MAIN: INVOKE OBJ 'M'. KEEP-GOING: DISPLAY 'OK'. STOP RUN.  SPEC (§14.9.23.4 GR5 → §14.6.13.1.3 #5, RESUME NOTE 2): OBJ is null → EC-OO-NULL set to exist → the declarative runs ('HANDLED'), RESUME transfers to KEEP-GOING ('OK'), exit 0.  ACTUAL: RequireNonNull throws, uncaught → 'abnormal run-unit termination: ...' on stderr, exit 1; neither 'HANDLED' nor 'OK' printed.  ||  EC-OO-METHOD (must use the UNIVERSAL path, since a typed receiver resolves the method name at compile time): `>>TURN EC-OO-METHOD CHECKING ON` · 01 U USAGE OBJECT REFERENCE (universal), INVOKE C 'NEW' RETURNING U, then `INVOKE U 'NOPE'` where class C declares no method NOPE. SPEC (GR7b → §14.6.13.1.3 #5): __CobolInvoke default arm sets EC-OO-METHOD to exist → USE AFTER EXCEPTION CONDITION EC-OO-METHOD declarative runs. ACTUAL: unconditional throw, uncaught, abnormal termination. Editions: 2002+ (OO).

### CA13 · [MAJOR/S] · editions-gating · ✅ LANDED (DEVLOG 999)
- **Spec:** Annex E §E.3.3 item 33 + §E.3.1 scope statement; §11.9.10 (OPTIONS INITIALIZE clause)
- **Verified:** Independently verified against the spec and code. Annex E (specs/ISO_COBOL.md:48507) is the list of substantive changes 'between the previous COBOL standard [2014] and this [2023]'. Item 33 (line 49766) reads: 'INITIALIZE clause of the OPTIONS paragraph. The content of data items that were not initialized explicitly was implementor-defined. The content is explicitly defined when this clause is specified.' It sits in E.3.3 'Not affecting' (header line 48929). E.3.1 (line 48892) states E.3 items 'have no syntax or semantic changes that might impact existing conforming source programs' (except new-word additions, which go to E.2). Derivation: if OPTIONS INITIALIZE were a pre-existing 2014 clause whose semantics were tightened in 2023 (implementor-defined -> explicitly defined content), that WOULD impact existing programs specifying it and would belong in E.2 'potentially affecting' — precisely where ISO placed the parallel VALUE-clause tightenings (E.2 items 27/28/29, lines 48848/48856/48864). Its E.3.3 'not affecting' placement is coherent ONLY if the clause is NEW in 2023 (no existing program uses a clause that did not exist), using already-reserved words (INITIALIZE/OPTIONS) so it needs neither E.2 nor E.3.2. Corroboration: (1) the repo's own docs/VERSION_CHANGE_REFERENCE.md row 76 disposition column literally reads 'new-feature-gate'; (2) every OTHER 2014 OPTIONS clause is dated correctly (DEFAULT ROUNDED/ENTRY-CONVENTION/FLOAT-BINARY/FLOAT-DECIMAL/INTERMEDIATE ROUNDING) — INITIALIZE was mis-lumped into that P10 batch. CODE: constructs.json:517 sets introducedIn=2014 -> ConstructRegistry.g.cs:56 new("options-initialize-2014","OPTIONS INITIALIZE",2014,null,null,"COBOLNET0900",...) -> StatusAt(2014)=Available. VisitOptionsInitializeClause (VersionConformancePass.cs:1424-1426) is the sole gate; the grammar arm is ungated. Confirmed divergence: at --std 2014 (default) a program with OPTIONS INITIALIZE compiles with NO diagnostic; at --std 2002/85 the introduction diagnostic names 'requires COBOL-2014' instead of 'requires COBOL-2023'. Residual caveat: the definitive tiebreaker would be the ISO/IEC 1989:2014 body text (not in-repo); under spec-first doctrine the only in-spec provenance is Annex E, which points to 2023.
- **Fix:** PRIMARY: tests/version-matrix/constructs.json — the 'options-initialize-2014' row (id at :511, introducedIn at :517): set introducedIn to 2023; rename the id to 'options-initialize-2023' and fix the description/vcr text that calls it 'a 2014 clause of the 2002 OPTIONS paragraph' (it is a 2023 clause). Then re-run scripts/gen-constructs.ps1 to regenerate ConstructRegistry.g.cs:56 (-> ...,"OPTIONS INITIALIZE",2023,...) and Constructs.g.cs:54 (-> OptionsInitialize2023). SECONDARY: src/Cobol.Net.Compiler/Validation/VersionConformancePass.cs:1424-1426 — update the doc-comment ('a 2014 clause' -> 'a 2023 clause of the 2002 OPTIONS paragraph') and change the Constructs.OptionsInitialize2014 reference to OptionsInitialize2023; the Check(...) call is otherwise correct. Mechanism: with introducedIn=2023 and removedIn=null, StatusAt(<2023)=NotYetIntroduced -> EditionSeverityPolicy.For=Error on BOTH axes and ConstructRegistry.Check emits EditionCodes.Introduction (COBOLNET0900) naming COBOL-2023. No behavioral/runtime code changes; this is a pure edition-registry correction.
- **Golden (spec-derived):** PROGRAM (min): 'IDENTIFICATION DIVISION.\nPROGRAM-ID. OINIT.\nOPTIONS.\n    INITIALIZE ALL TO SPACES.\nDATA DIVISION.\nWORKING-STORAGE SECTION.\n01 W PIC 9(3) VALUE 7.\nPROCEDURE DIVISION.\nMAIN.\n    DISPLAY W.\n    STOP RUN.' — Spec-derived expectations (rule: Annex E item 33 = a 2014->2023 introduction of the §11.9.10 clause): at --std 2023 accepted, DISPLAY prints '007'. At --std 2014 (DEFAULT/strict) EXPECTED = REJECT COBOLNET0900 'the OPTIONS INITIALIZE clause requires COBOL-2023 (targeting COBOL-2014)'; CURRENT code accepts and prints '007' (the bug). At --std 2002 and --std 85 EXPECTED = REJECT naming 'requires COBOL-2023'; CURRENT code names 'requires COBOL-2014'. Editions affected: introduction boundary 2023 — reject at 85/2002/2014.

### CA15 · [MAJOR/M] · files-io · ✅ LANDED (DEVLOG 1000)
- **Spec:** §14.9.30.4 GR15 (READ, line sequential over-length); §9.1.13.2 item 5 ('06'); NOTE 3
- **Verified:** SPEC: §14.9.30 GR15 — 'If the number of bytes in the record that is read is greater than the maximum size specified by the record description entries..., the record is truncated on the right to the maximum size. In that case, the READ statement is successful and the I-O status in the read file connector is set to [0]6... After the read the file position indicator will reference the next unread character in the record.' NOTE 3: subsequent READs read the rest of the record up to the line delimiter. §9.1.13.2 item 5 defines '06' for exactly this. CODE: SequentialConnector.Read (SequentialConnector.cs:380-386) — the `_lineSequential` branch calls `_reader.ReadLine()`, which consumes the ENTIRE physical line INCLUDING its delimiter, sets `LastReadLength = Math.Min(line.Length, RecordWidth)` and `image = Fit(line, RecordWidth)` (truncating). The `bool shortLong` flag that drives the status at :419 (`Status = shortLong ? RecordLengthShortLong : Success`) is ONLY ever set in the record-sequential (:415) and varying (:399) branches — never for line-sequential — so an over-length line always reports '00' and the truncated tail is silently discarded (the next READ reads the FOLLOWING physical line). There is no '06' constant in FileStatus.cs at all. Two distinct spec violations: wrong I-O status, and silent data loss + mis-positioning that corrupts every subsequent READ when a program uses the GR15/NOTE-3 multi-read pattern. VERIFIED independently against both the spec text and the code path; this is a real conformance bug (auditor's cited lines are accurate).
- **Fix:** SequentialConnector.cs: (1) FileStatus.cs — add `public const string LineRecordTooLong = "06";` (ISO §9.1.13.2 item 5). (2) Add a field near line 21: `private string? _lineRemainder;` and reset it in OpenCore alongside the other reset state (after line 191, `_readOffset = 0;`): `_lineRemainder = null;`. (3) Replace the `_lineSequential` READ branch (lines 380-386) so it services a pending remainder before reading a new physical line, and splits an over-length line into RecordWidth chunks: read `line` from `_lineRemainder` (clearing it) if non-null, else `_reader.ReadLine()` (null ⇒ AtEnd as today); then if `line.Length > RecordWidth` set `_lineRemainder = line[RecordWidth..]`, `image = line[..RecordWidth]`, `LastReadLength = RecordWidth`, and a new local `bool lineTooLong = true`; else `LastReadLength = line.Length; image = Fit(line, RecordWidth);`. (4) At line 419 fold the new flag in: `Status = lineTooLong ? FileStatusCode.LineRecordTooLong : shortLong ? FileStatusCode.RecordLengthShortLong : FileStatusCode.Success;`. Mechanism: `_lineRemainder` models the GR15 file-position-indicator 'next unread character in the record'; each over-length chunk yields '06', the final ≤RecordWidth chunk yields '00' (GR15 short-case padding is already handled by Fit). The over-length read stays successful (`PrevOpWasSuccessfulRead = true; _readOrdinal++` at :417-418 unchanged).
- **Golden (spec-derived):** PROGRAM-ID LSLONG. SELECT F ASSIGN "lslong.txt" ORGANIZATION LINE SEQUENTIAL FILE STATUS FS. FD F. 01 REC PIC X(5). 01 FS PIC XX (WS). Procedure: OPEN INPUT F. READ F. DISPLAY "1:" REC " FS=" FS. READ F. DISPLAY "2:" REC " FS=" FS. READ F AT END DISPLAY "E FS=" FS END-READ. CLOSE F. STOP RUN. INPUT lslong.txt = one physical line 'ABCDEFGH' (8 chars) + newline. SPEC-DERIVED EXPECTED (§14.9.30 GR15 + NOTE 3): READ1 truncates 8→5 = 'ABCDE', FS='06', FPI at next unread char ⇒ '1:ABCDE FS=06'; READ2 continues the remainder 'FGH' (3≤5) padded ⇒ '2:FGH   FS=00'; READ3 hits EOF ⇒ 'E FS=10'. (Editions: version-invariant — line sequential + '06' present since COBOL-2002/2014; behavior unchanged in 2023.) CODE ACTUAL (buggy): '1:ABCDE FS=00' then READ2 skips to next line (none) ⇒ 'E FS=10' with 'FGH' permanently lost.

### CA16 · [MAJOR/S] · files-io · ✅ LANDED (DEVLOG 1000)
- **Spec:** §14.9.27 GR14 + GR17 (OPEN I-O optional-absent creates the file, FPI=1); §14.9.30.4 GR21 sequential rule e + GR24 ('10'); §9.1.13.7 item 7 ('47' is only for NOT open input/I-O)
- **Verified:** SPEC: §14.9.27 GR17 — an absent OPTIONAL file opened I-O 'creates the file' as if 'OPEN OUTPUT; CLOSE' then the OPEN I-O, and sets I-O status '05'. GR14 — for a sequential file opened INPUT/I-O the file position indicator is set to 1 (GR13's 'optional input file is not present' FPI is set ONLY for the INPUT phrase, NOT I-O). §14.9.30 GR21 (sequential) — a first READ on the now-empty file finds no record (rule e), the at-end condition exists, and GR24a sets I-O status '10'. §9.1.13.7 item 7 defines '47' as 'READ/START attempted referencing a file connector that is NOT open in the input or I-O mode' — but here the connector IS open in I-O mode, so '47' is categorically wrong. CODE: SequentialConnector OpenCore IO branch (SequentialConnector.cs:236-242) — for `!exists && IsOptional` the `else` at :240 creates ONLY a `_writer`, never a `_reader`, and does NOT set OptionalAbsent, returning '05'. Then Read (:367): IsOpen is true (`_writer` non-null), Mode==IO passes the Output/Extend guard, OptionalAbsent is false, LastReadUnsuccessful is false, so it reaches `if (_reader is null) { Status = ReadNotOpenForInput; return false; }` at :374 ⇒ '47'. VERIFIED the INPUT branch (:203) correctly sets OptionalAbsent=true (yielding '10'), and that RelativeConnector (:113-118) and IndexedConnector (:148-155) both create an empty store on I-O-absent and read from an in-memory structure ⇒ first READ correctly yields '10'. The sequential connector is the sole outlier. Real bug: '47' vs '10' flips control flow — the emitted AT END test (FileStatus[0]=='1') is false for '47', so AT END never fires and a NOT AT END branch runs / control mis-routes as an open-mode error for a fully conformant program.
- **Fix:** SequentialConnector.cs:236-242 — rewrite the IO OpenCore branch so it always ends with a ReadWrite `_reader`, creating the file first when absent (§14.9.27 GR17). Replace lines 236-241 with:
    if (!exists && !IsOptional) return FileStatusCode.FileNotFound;
    if (!exists) using (new StreamWriter(HostPath, append: false, Encoding.Latin1)) { }   // §14.9.27 GR17 create as if OPEN OUTPUT + CLOSE
    _reader = new StreamReader(new FileStream(HostPath, FileMode.Open, FileAccess.ReadWrite,
        SharedStreams ? FileShare.ReadWrite : FileShare.Read), Encoding.Latin1);
    if (!exists && IsOptional) return FileStatusCode.OptionalFileNotFound;
    break;
Mechanism: after creation the empty file is opened I-O exactly like the existing-file path, so the FPI is effectively 'at 1' (GR14); the first READ's FillChars/ReadLine returns 0/null ⇒ AtEnd '10' (GR21 rule e + GR24), and a same-length in-place REWRITE still works through `_reader.BaseStream`. OPEN still returns '05'. No `OptionalAbsent` needed (the file now physically exists, empty).
- **Golden (spec-derived):** PROGRAM-ID OPTIO. SELECT OPTIONAL F ASSIGN "optabsent.dat" ORGANIZATION SEQUENTIAL FILE STATUS FS. FD F. 01 REC PIC X(10). 01 FS PIC XX (WS). Procedure: OPEN I-O F. DISPLAY "OPEN FS=" FS. READ F AT END DISPLAY "ATEND FS=" FS NOT AT END DISPLAY "GOTREC FS=" FS END-READ. CLOSE F. STOP RUN. Precondition: optabsent.dat does NOT exist. SPEC-DERIVED EXPECTED (§14.9.27 GR17 create ⇒ '05'; GR14 FPI=1; §14.9.30 GR21 rule e + GR24 empty-file at-end ⇒ '10'): 'OPEN FS=05' then 'ATEND FS=10'. (Editions: OPTIONAL + I-O create behavior is version-invariant across 85/2002/2014/2023.) CODE ACTUAL (buggy): 'OPEN FS=05' then FS='47' ⇒ AT END does not fire (status[0]!='1'); 'GOTREC FS=47' prints instead.

### CA2 · [MAJOR/M] · accept-display-misc · ✅ LANDED (DEVLOG 996)
- **Spec:** §14.9.20.4 GR4 ("If the category of a receiving-operand is data-pointer, function-pointer, message-tag, object-reference, or program-pointer, the implicit statement is: SET receiving-operand TO sending-operand", spec line 27603-27605), GR5a1 (these categories are NOT excluded, spec line 27615), GR6c fill table (Data-pointer/Program-pointer → predefined address NULL, Object-reference → predefined object reference NULL, spec lines 27669-27677); §14.9.20.3 SR1 (identifier-1 may be class pointer/object, spec line 27562)
- **Verified:** SPEC: a data-pointer/object-reference/program-pointer receiving-operand under INITIALIZE gets an implicit SET…TO NULL; GR5a1 explicitly keeps these categories as receiving operands (not MOVE-receiver-excluded), and a bare INITIALIZE (GR5c4) qualifies every non-excluded item. CODE: InitializeItemCategory (InitializeBinder.cs:214-225) has NO arm for PicCategory.Pointer / PicCategory.ProgramPointer / PicCategory.ObjectReference (PicInfo.cs:35/40/46) — all three fall to `_ => null` (line 224); ExpandInitialize (line 147) treats a null category as excluded and `return`s, so nothing is emitted. Verified for a bare `INITIALIZE pp` (elementary, identifier1=true — GR5a2 skip does not apply, still hits the null-category return) and for a pointer/object-ref member of an INITIALIZEd group (the child recursion hits the same return). InitializeEmitter handles only InitializeStore/Loop/DynLoop/Error — there is no NULL-set path at all. So the item silently retains its prior value instead of being reset to NULL. The correct NULL values already exist as PicInfo.DefaultInitializer (PicInfo.cs:325-327: ObjectReference→"null", Pointer→"ManagedPointer.Null", ProgramPointer→"ProgramPointer.Null").
- **Fix:** Three-part (SET is NOT a MOVE, so it must not route through move.Emit/ConvertSource): (1) E:\CobolSharp\src\Cobol.Net.Compiler\Binding\Procedure\Verbs\InitializeBinder.cs, InitializeItemCategory (line 214-225): add arms `{ Category: PicCategory.Pointer } => InitializeCategory.DataPointer`, `{ Category: PicCategory.ProgramPointer } => InitializeCategory.ProgramPointer`, `{ Category: PicCategory.ObjectReference } => InitializeCategory.ObjectReference` (add these three InitializeCategory enum members). (2) In ExpandInitialize's elementary arm (around line 144-150): when `cat` is one of these three, run the SAME GR5c qualification test as InitializeSender (bare / DEFAULT / VALUE-category-match / REPLACING-category-match); if it qualifies, for the bare/DEFAULT/VALUE cases emit a new action `InitializeSetNull(cur.ToPlace())` (the GR4 SET…TO NULL with GR6c's predefined-NULL sending operand), and for the REPLACING-category-match case emit a SET place TO identifier-2 (GR6b). (3) E:\CobolSharp\src\Cobol.Net.Compiler\CodeGen\Verbs\InitializeEmitter.cs EmitAction (near line 38): add `case InitializeSetNull s: w.Line(PlaceRenderer.Write(s.Target, s.Target.Item.Pic!.DefaultInitializer)); break;` — reuses the existing predefined-NULL idiom (PicInfo.cs:325-327), matching SetEmitter.EmitSetPointer/EmitSetProgramPointer and OoEmitter.EmitSetObjectRef (src="null"). Add the `InitializeSetNull(Place Target) : InitializeAction` record in Binding/Bound/BoundInitialize.cs and its UsageCollectionPass.cs:243 case.
- **Golden (spec-derived):** Edition COBOL-2002+ (program-pointer is a 2002 feature). Program:
 IDENTIFICATION DIVISION.
 PROGRAM-ID. INITPP.
 DATA DIVISION.
 WORKING-STORAGE SECTION.
 01 PP USAGE PROGRAM-POINTER.
 PROCEDURE DIVISION.
 MAIN.
     SET PP TO ENTRY "INITSUB".
     IF PP = NULL DISPLAY "BEFORE-NULL" ELSE DISPLAY "BEFORE-SET" END-IF.
     INITIALIZE PP.
     IF PP = NULL DISPLAY "AFTER-NULL" ELSE DISPLAY "AFTER-SET" END-IF.
     STOP RUN.
 END PROGRAM INITPP.
 IDENTIFICATION DIVISION.
 PROGRAM-ID. INITSUB.
 PROCEDURE DIVISION.
     GOBACK.
 END PROGRAM INITSUB.
SPEC-DERIVED EXPECTED (§14.9.20.4 GR4 + GR6c — INITIALIZE PP emits SET PP TO NULL): line1 BEFORE-SET, line2 AFTER-NULL.
CODE PRODUCES (buggy): line1 BEFORE-SET, line2 AFTER-SET (INITIALIZE PP emits nothing; PP keeps the entry address). An OBJECT REFERENCE member of an INITIALIZEd group diverges identically (stays pointing at its prior object instead of resetting to the predefined NULL reference).

### CA21 · [MAJOR/S] · interprogram · ✅ LANDED (DEVLOG 1084)
- **Spec:** §14.9.4.4 GR3b (specs/ISO_COBOL.md:25842); GR3g (:25866); Table 13 (:24456); §14.9.4.4 GR3h#2 (:25872)
- **Verified:** Spec: §14.9.4.4 GR3b — 'If the data item referenced by identifier-1 contains the predefined address NULL, the EC-PROGRAM-PTR-NULL exception condition is set to exist. If the program cannot be located or identifier-1 references a zero-length item, the EC-PROGRAM-NOT-FOUND exception condition is set to exist.' These are two DISTINCT conditions; the NULL case is specifically EC-PROGRAM-PTR-NULL. GR3g's 'invalid program address ... execution is undefined' (:25866) applies to a NON-null pointer holding a bad address (e.g. a pointer to a CANCELed program, :25984), NOT to NULL. Table 13 (:24456): EC-PROGRAM-PTR-NULL = Fatal. Code: ProgramTable.CallPointer (ProgramTable.cs:247-252) does `if (target.IsNull) throw new CobolCallException("...this implementation raises EC-PROGRAM-NOT-FOUND)", "EC-PROGRAM-NOT-FOUND")` — it raises the wrong name (EC-PROGRAM-NOT-FOUND), conflating GR3b's null case with the not-located case. EC-PROGRAM-PTR-NULL IS registered (ExceptionCatalog.cs:156, Fatal) but is set to exist at NO CALL site. Code also diverges from its own design doc (COBOLNET_INTERPROGRAM_DESIGN.md:137: 'CALL to a NULL program-pointer → EC-PROGRAM-PTR-NULL'). A program that enables/handles ONLY EC-PROGRAM-PTR-NULL (USE AFTER EXCEPTION CONDITION EC-PROGRAM-PTR-NULL, or FUNCTION EXCEPTION-STATUS) never sees it. Interlocks with CA22 — both are required for the >>TURN + USE-declarative scenario to work end-to-end.
- **Fix:** ProgramTable.cs:247-251 — in CallPointer's null branch, raise the correct name: change the CobolCallException EcName argument from "EC-PROGRAM-NOT-FOUND" to "EC-PROGRAM-PTR-NULL" and reword the message to cite §14.9.4.4 GR3b (NULL program-pointer → EC-PROGRAM-PTR-NULL, Table 13 Fatal) instead of the current 'invalid program address / EC-PROGRAM-NOT-FOUND' text. Localized: the non-null path (delegating to CallProgram) is unchanged, and CallPointer takes no notFoundEc parameter so no caller threading is needed. Must ship together with the CA22 fix.
- **Golden (spec-derived):** Default --std 2023 (program-pointers + EC machinery are 2002+). `IDENTIFICATION DIVISION. PROGRAM-ID. CA21NUL. DATA DIVISION. WORKING-STORAGE SECTION. 01 PPTR USAGE PROGRAM-POINTER. PROCEDURE DIVISION. DECLARATIVES. H SECTION. USE AFTER EXCEPTION CONDITION EC-PROGRAM-PTR-NULL. H-BODY. DISPLAY "HANDLED-PTR-NULL". END DECLARATIVES. M SECTION. M0.` then directive line `>>TURN EC-PROGRAM-PTR-NULL CHECKING ON` then `SET PPTR TO NULL. CALL PPTR. DISPLAY "AFTER". STOP RUN.`  Spec-DERIVED expected: SET makes PPTR the predefined address NULL; `CALL PPTR` sets EC-PROGRAM-PTR-NULL to exist (GR3b); checking is ON with no ON EXCEPTION phrase, so GR3h#2 executes the applicable exception-processing statements → the USE AFTER EXCEPTION CONDITION EC-PROGRAM-PTR-NULL declarative runs → prints `HANDLED-PTR-NULL`; the declarative completes normally and EC-PROGRAM-PTR-NULL is Fatal (Table 13) so §14.6.13.1.3 #5 terminates the run unit abnormally (nonzero exit); `AFTER` is NOT printed. Expected stdout = `HANDLED-PTR-NULL` + abnormal termination. Current buggy: CallPointer raises EC-PROGRAM-NOT-FOUND (never matches the PTR-NULL declarative) AND (CA22) the CALL is emitted with no EC scaffolding, so the CobolCallException propagates uncaught → abnormal termination with NEITHER line printed.

### CA22 · [MAJOR/S] · interprogram · ✅ LANDED (DEVLOG 1084)
- **Spec:** §14.9.4.4 GR3b (:25842) + GR3h#2 (:25872); §14.6.13.1 / Table 13 (EC-PROGRAM-PTR-NULL, :24456)
- **Verified:** Spec: GR3b sets EC-PROGRAM-PTR-NULL for a NULL program-pointer CALL; GR3h#2 — when checking for the (EC-PROGRAM) condition is enabled and no ON EXCEPTION phrase is present, the applicable exception-processing statements (the USE declarative / F3 WHEN) execute. EC-PROGRAM-PTR-NULL is a checkable level-3 name (Table 13; ExceptionCatalog.cs:156). Code: EcBinder.ProgramNames (EcBinder.cs:283-286) = [EC-PROGRAM-NOT-FOUND, EC-PROGRAM-RECURSIVE-CALL, EC-PROGRAM-CANCEL-ACTIVE, EC-PROGRAM-ARG-OMITTED] — EC-PROGRAM-PTR-NULL is OMITTED. EcWrap's QueryFor(BoundCallProgram) (EcBinder.cs:354-356) calls Query(ProgramNames) + Query(ExternalNames), so a CALL under `>>TURN EC-PROGRAM-PTR-NULL CHECKING ON` with no other EC-PROGRAM name enabled matches nothing → enabled.Count==0 → EcWrap (line 413) returns the statement UNWRAPPED (no BoundEcChecked). At emit, EnabledProgramNames() (CallEmitter.cs:115-118) reads a null ecState.Info → ecProg empty; with no ON/NOT ON phrase, `!hasPhrase && ecProg.Count==0` (CallEmitter.cs:66) emits the BARE invocation with no try/catch (CallEmitter.cs:68). So even after the CA21 name fix, the runtime CobolCallException has no catch → propagates to RunMain → AbnormalTermination; the matching USE declarative / F3 WHEN is never selected and RESUME is impossible. Verified the emitter side is otherwise ready: EnabledProgramNames filters on the 'EC-PROGRAM-' prefix (CallEmitter.cs:116), which EC-PROGRAM-PTR-NULL satisfies, and EmitProgramEcCatch (CallEmitter.cs:143) builds the `__ce.EcName == "EC-PROGRAM-PTR-NULL"` when-filter — so adding the name to the binder array alone closes the gap. Interlocks with CA21 (the runtime must also raise the correct name). Note: the ON EXCEPTION-phrase path is unaffected (its `catch (CobolCallException)` at :79 is name-agnostic); the gap is specifically the >>TURN/USE-declarative path.
- **Fix:** EcBinder.cs:283-286 — add "EC-PROGRAM-PTR-NULL" to the ProgramNames array (the EC-PROGRAM family a CALL raises through CobolCallException). This makes EcWrap query its TURN enablement for a BoundCallProgram, wrap the CALL in BoundEcChecked when it is enabled, and the emitter's existing EC-PROGRAM- prefix filter (CallEmitter.cs:116) + EmitProgramEcCatch (CallEmitter.cs:143) then generate the name-filtered catch that dispatches to the USE declarative / F3 handler. Must ship with the CA21 runtime-name fix (otherwise the catch's `EcName == "EC-PROGRAM-PTR-NULL"` guard never matches the still-wrong runtime name). Adjacent (not part of this finding, but note for the fix commit): EC-PROGRAM-ARG-MISMATCH and EC-PROGRAM-RESOURCES are likewise absent from ProgramNames, but neither has a CobolCallException raise site today, so they are lower-priority and out of CA22 scope.
- **Golden (spec-derived):** Same program as the CA21 golden (the two findings share one scenario and one runtime path). Spec-DERIVED expected: `HANDLED-PTR-NULL` printed, then abnormal termination (EC-PROGRAM-PTR-NULL is Fatal, §14.6.13.1.3 #5). CA22-specific mechanism demonstrated: with ONLY EC-PROGRAM-PTR-NULL enabled and no ON EXCEPTION phrase, the binder must wrap the CALL so the emitter produces the `try { CallPointer(...) } catch (CobolCallException __ce) when (__ce.EcName == "EC-PROGRAM-PTR-NULL") { ExceptionState.Set(...); __EcDispatch(...) }` scaffolding that runs the declarative. Current buggy: no BoundEcChecked wrapper → bare `ProgramRegistry.CallPointer(...)` with no catch → the CobolCallException reaches RunMain uncaught → abnormal termination with `HANDLED-PTR-NULL` never printed. Positive contrast: if EC-PROGRAM-NOT-FOUND were ALSO enabled, the CALL WOULD be wrapped (NOT-FOUND is in ProgramNames) and the current wrong-name raise would be caught by the NOT-FOUND catch arm — but the EC-PROGRAM-PTR-NULL USE declarative still would not match, proving CA21+CA22 must be fixed jointly.

### CA23 · [MAJOR/M] · intrinsics · ✅ LANDED (DEVLOG 1003)
- **Spec:** §15.59.4 r1 (MAX), §15.63.4 r1 (MIN), §15.71.4 (ORD-MAX), §15.72.4 (ORD-MIN) — 'comparisons ... made according to the rules for simple conditions (see 8.8.4.2)'; §8.8.4.2.7 (alphanumeric comparison is with respect to the current alphanumeric program collating sequence); §8.8.4.2.9 (national)
- **Verified:** Spec: MAX/MIN/ORD-MAX/ORD-MIN determine the greatest/least value by §8.8.4.2 relation-condition rules, and §8.8.4.2.7 requires alphanumeric comparison to use the current alphanumeric PROGRAM COLLATING SEQUENCE (national → §8.8.4.2.9 national PCS). Code: CobolIntrinsics.Exact.cs:151-182 MaxString/MinString/OrdMaxString/OrdMinString compare with string.CompareOrdinal (raw UTF-16 code order); IntrinsicBinder.cs:259-270 resolves the string overloads but the collate/collateNat flags (:281-283) are wired ONLY for CHAR/ORD, never MAX/MIN/ORD-MAX/ORD-MIN, and there is no weights-taking runtime overload; IntrinsicRenderer.cs:133-134/327-328 pass only StrArgList. So a non-default PCS is ignored, and MAX/MIN DISAGREE with relation conditions in the same program (ConditionRenderer.cs:147 routes CobolString.Compare(..., ctx.CollateArg=__COLLATE)). Verified the collation seam exists and is unused here: CobolString.Compare(string,string,ushort[]) (256-entry PCS weights) and Compare(string,string,NationalCollation). Secondary defect: string.CompareOrdinal does not space-pad unequal-length operands (§8.8.4.2.7 r2), so MAX over different-width args is wrong even under the native PCS.
- **Fix:** (1) Runtime CobolIntrinsics.Exact.cs:151-182 — replace `string.CompareOrdinal(a,b)` with `CobolString.Compare(a,b)` (space-pads per §8.8.4.2.7 r2) for the default, and ADD weights overloads `MaxString(ushort[] w, params string[] xs)` etc. using `CobolString.Compare(x,m,w)`, plus national overloads `MaxString(NationalCollation nat, params string[] xs)` using `CobolString.Compare(x,m,nat)`. (2) Binder IntrinsicBinder.cs:259-270 — for MAX/MIN/ORD-MAX/ORD-MIN set `collate` (a non-identity alphanumeric PCS in effect AND alphanumeric args) / `collateNat` (a non-native national PCS AND national args), mirroring the CHAR/ORD logic at :281-283, and select the weighted/national RuntimeMethod variant. (3) Renderer IntrinsicRenderer.cs:133-134,327-328 — append `Collate(ic)` (the existing '__COLLATE'/'__COLLATE_NAT' fragment, :298-299) after StrArgList for these four methods. Default/native PCS still emits the parameterless overload (identity weights → byte-stable).
- **Golden (spec-derived):** IDENTIFICATION DIVISION. PROGRAM-ID. CA23. ENVIRONMENT DIVISION. CONFIGURATION SECTION. SPECIAL-NAMES. ALPHABET AL IS "ZYXWVUTSRQPONMLKJIHGFEDCBA". OBJECT-COMPUTER. XX PROGRAM COLLATING SEQUENCE AL. DATA DIVISION. WORKING-STORAGE SECTION. 01 A PIC X VALUE "A". 01 Z PIC X VALUE "Z". PROCEDURE DIVISION. DISPLAY FUNCTION MAX(A Z). IF A > Z DISPLAY "A-GT-Z" ELSE DISPLAY "Z-GT-A". STOP RUN. — Spec-derived: ALPHABET AL orders Z at position 1 (lowest) ... A at position 26 (highest), so under §8.8.4.2.7 'A' collates ABOVE 'Z'. §15.59.4 r1 → MAX returns "A"; the relation condition prints "A-GT-Z". Current code: string.CompareOrdinal ranks 'Z'(90) > 'A'(65) so MAX prints "Z", while the relation condition (which DOES honor AL) prints "A-GT-Z" — MAX contradicts the program's own relation. ORD-MAX/ORD-MIN diverge identically. Editions: all (85+).

### CA24 · [MAJOR/S] · intrinsics · ✅ LANDED (DEVLOG 1001)
- **Spec:** §15.34.3 r1 / §15.35.3 r1 (sole argument rule = class numeric; no upper bound); §15.34.4/§15.35.4 (returned value = e**arg / 10**arg); §15.3 (EC-ARGUMENT-FUNCTION is only for argument/return-value RULE violations); §15.55.3 r2 / §15.56.3 r2 (LOG/LOG10 domain > 0)
- **Verified:** Spec: EXP/EXP10's ONLY argument rule is 'class numeric' — no upper bound — so a large finite argument (EXP(710)) is fully LEGAL; its equivalent-expression value e^710 ≈ 2.25e308 is a correct (merely large) numeric value that undergoes ordinary receiver SIZE handling (§14.7.4), never a domain error. Code: CobolIntrinsics.cs:32-35 FromDouble maps double.IsInfinity identically to NaN → Exceptions.ExceptionState.ArgumentError → returns the §15.3 default 0 (EC-ARGUMENT-FUNCTION checking off) or throws a FATAL EC-ARGUMENT-FUNCTION (checking on). Math.Exp(710)=+∞ (binary64 overflow) reaches CobolIntrinsics.Float.cs:31-32 then FromDouble, so a legal argument yields the WRONG value (0) and the WRONG exception. Note the code already correctly saturates FINITE over-range results (EXP10(19)=1e19 → long.MaxValue), so ±∞ is the sole leak. The one coupling to preserve: LOG(0)=-∞ / LOG10(0)=-∞ ARE genuine §15.55.3 r2 domain violations that must stay EC-ARGUMENT-FUNCTION.
- **Fix:** Two coupled edits. (1) CobolIntrinsics.cs:32-35 — split the guard: `if (double.IsNaN(d)) return ArgumentError(...);` then `if (double.IsInfinity(d)) return d > 0 ? long.MaxValue : long.MinValue;` (a legal-argument overflow saturates like a finite over-range value, letting the receiver store raise EC-SIZE-* / truncate). (2) CobolIntrinsics.Float.cs:29-30 Log/Log10 — add the §15.55.3 r2 / §15.56.3 r2 domain guard `x <= 0 ? Exceptions.ExceptionState.ArgumentError(...) : Math.Log(x)` so their domain -∞ is raised at the body as EC-ARGUMENT-FUNCTION, NOT saturated by the now-permissive FromDouble. (Both changes are required together; without (2), LOG(0) would wrongly saturate.) No renderer change needed. Alternative narrower fix: pass an `overflowOk:true` flag from RenderFloat (IntrinsicRenderer.cs:243-245) only for EXP/EXP10 and saturate ±∞ only then — but the guard approach is more general and spec-faithful (each function enforces its own §15.x.3 domain).
- **Golden (spec-derived):** IDENTIFICATION DIVISION. PROGRAM-ID. CA24. DATA DIVISION. WORKING-STORAGE SECTION. 01 R PIC 9(5) VALUE 7. PROCEDURE DIVISION. COMPUTE R = FUNCTION EXP(710) ON SIZE ERROR DISPLAY "SIZE" NOT ON SIZE ERROR DISPLAY "OK". DISPLAY R. STOP RUN. — Spec-derived (§15.34.3 r1: 710 is a legal class-numeric argument; §15.34.4: value ≈ 2.25e308 hugely exceeds R's 5-digit capacity → §14.7.4 SIZE ERROR): prints `SIZE` and R is unchanged (7). Current code: FromDouble(+∞)→ArgumentError→0 (checking off), 0 fits R, so NOT-ON-SIZE-ERROR fires → prints `OK` then `00000`. With `>>TURN EC-ARGUMENT-FUNCTION CHECKING ON` the current code additionally throws a fatal EC-ARGUMENT-FUNCTION and abnormally terminates the run unit, where the spec never sets that condition. Editions: 2002/2014/2023 (EXP/EXP10 introducedIn 2002).

### CA25 · [MAJOR/M] · intrinsics · ✅ LANDED (DEVLOG 1004)
- **Spec:** §15.97.1 (UPPER-CASE), §15.57.1 (LOWER-CASE), §15.78.1 (REVERSE) result-type tables: National argument → National function type; §14.9.25.4 Table 16 (National sending operand → Alphanumeric receiver = 'No'); §14.9.25.3 SR10
- **Verified:** Spec: each type table maps a National argument to a National function type — the result category MUST follow the argument. Code: IntrinsicCatalog.cs:125-127 hardcodes LOWER-CASE/UPPER-CASE/REVERSE to IntrinsicType.Alphanumeric, so IntrinsicSig.ResultCategory is always PicCategory.Alphanumeric; IntrinsicBinder.cs:257 sets `category = sig.ResultCategory` with polymorphic resolution ONLY for the 'p' MAX/MIN family, never these 's' functions. A national argument (PIC N) reverses/case-folds correctly in the UTF-16 string channel (VALUE is right), but the result is labelled category Alphanumeric. Verified downstream impact: MoveBinder.cs:162 takes an intrinsic sender's category straight from ic.ResultCategory, and MoveCategoryLegality (:220-227) rejects National→Alphanumeric MOVE with COBOLNET0819 — so mis-labelling as Alphanumeric BYPASSES that Table-16 guard and also feeds the wrong class to comparison collation (national vs alphanumeric). Same defect pattern extends to TRIM (§15.96), CONCAT, SUBSTITUTE, and the FORMATTED-* / CURRENT-DATE family whose type tables likewise follow the argument (e.g. §15.41.1) — the catalog fixes them all to Alphanumeric.
- **Fix:** Make these rows category-polymorphic like MAX/MIN. Preferred: in IntrinsicBinder.cs (around :254-270, after args bind) resolve the result category from the §15.x.1 table for the 's'-argument case-preserving/reversing functions — when the (sole) argument's OperandCategory (already computed at :500-508) is National, set `category = PicCategory.National` and swap RuntimeMethod to the same body (Reverse/UpperCase/LowerCase are code-unit ops that already work on the national UTF-16 string). Mechanism mirrors the MAX/MIN 'p' block. Add a catalog flag (e.g. an ArgKinds 'q' = category-follows-argument, or a bool NationalPolymorphic) on the LOWER-CASE/UPPER-CASE/REVERSE (and TRIM/CONCAT/SUBSTITUTE/FORMATTED-*/CURRENT-DATE) rows so the resolution is table-driven, not per-name. RenderString already routes MaxString/Reverse/etc. by RuntimeMethod, and a National ResultCategory correctly reaches the string channel (RenderNum:54 stays loud for a national result in a numeric context, which is correct).
- **Golden (spec-derived):** IDENTIFICATION DIVISION. PROGRAM-ID. CA25. DATA DIVISION. WORKING-STORAGE SECTION. 01 N1 PIC N(3) VALUE N"ABC". 01 A PIC X(6). PROCEDURE DIVISION. MOVE FUNCTION REVERSE(N1) TO A. DISPLAY A. STOP RUN. — Spec-derived (§15.78.1: REVERSE(N1) is category National; §14.9.25.4 Table 16 National-row / Alphanumeric-column = 'No'): the program is REJECTED at compile time with COBOLNET0819 (MOVE of a national sending operand to an alphanumeric receiver is invalid — FUNCTION DISPLAY-OF is the sanctioned narrowing). Current code: REVERSE(N1) is categorized Alphanumeric, the MOVE compiles as a plain alphanumeric copy, and the raw national UTF-16 code units of the reversed value are stored into PIC X A. Editions: 2002/2014/2023 (PIC N and the National function type are 2002+).

### CA27 · [MAJOR/S] · move-convert · ✅ LANDED (DEVLOG 998)
- **Spec:** §14.9.25.4 GR5 + GR6d1 (de-editing) ; §14.9.25.3 SR10 + Table 16 (Numeric-edited sender → Numeric,Numeric-edited = Yes)
- **Verified:** SPEC: Table 16 (§14.9.25.3, p.696) lists the Numeric-edited sending row → 'Numeric, Numeric-edited' receiving column = Yes, so MOVE numeric-edited→numeric-edited is a LEGAL move. §14.9.25.4 GR5: 'De-editing takes place ... when the sending operand is a numeric-edited data item and the receiving item is a numeric OR a numeric-edited data item.' GR6d1: 'If the category of the sending operand is numeric-edited ... de-editing establishes the operand's numeric value, which may be signed.' So the sender's edited image must be DE-EDITED to its numeric value, then re-edited into the receiver mask (GR5/GR6). CODE: MoveEmitter.cs:311 gates the de-editing/edit arm on `case PicCategory.NumericEdited when IsNumericOperand(source)`. IsNumericOperand (MoveEmitter.cs:260-266) returns true for a BoundFieldOperand ONLY when `Pic?.Category is PicCategory.Numeric` (line 264) — a numeric-edited field has Category NumericEdited, so it returns FALSE. The numeric-edited source therefore falls through to the plain arm at :325-326, which does `EditFormat(NumFromAlphanumeric(AsString(src)), "0", receiverMask...)`. Verified: AsString of a numeric-edited field = its raw edited image (OperandText.cs:109-110 → PlaceRenderer.Read); CobolNum.FromAlphanumeric (CobolNum.cs:301-308) keeps only chars '0'-'9', discarding the '.' and any sign, yielding an UNSIGNED integer at scale 0. So SRC image ' 12.34' → 1234 (scale 0) → EditFormat into 999.99 → '234.00'. VERIFIED the correct path exists and works: the numeric-RECEIVER case (:382-384) already routes a numeric-edited source through `num.AsNum`, and AsNum→FieldNum (NumericRenderer.cs:125-131) de-edits it via CobolEdit.DeEdit at MaskScale — proving AsNum yields the true value 12.34 at scale 2. IsNumericOperand is referenced ONLY at :311 (grep-confirmed), so the two receiver categories (Numeric vs NumericEdited) disagree on the identical source purely because of the missing NumericEdited case in this one predicate.
- **Fix:** src/Cobol.Net.Compiler/CodeGen/Verbs/MoveEmitter.cs:264 — change the BoundFieldOperand arm of IsNumericOperand from `f.Place.Item.Pic?.Category is PicCategory.Numeric` to `f.Place.Item.Pic?.Category is PicCategory.Numeric or PicCategory.NumericEdited`, and update the method doc-comment (:257-259) to state that a numeric-edited source qualifies because GR5 de-editing yields a numeric value. MECHANISM: this makes a numeric-edited field satisfy the `when IsNumericOperand(source)` guard at :311, routing it through the de-editing arm (:312-320): `num.AsNum(source, ReceiverContext.None)` de-edits via CobolEdit.DeEdit (NumericRenderer.cs:129-131) to the true value at the mask scale (12.34, scale 2), which EditFormat then aligns and edits into the receiver mask (GR5/GR6/§14.6.8) → '012.34'. SAFE/LOCALIZED: IsNumericOperand is used at exactly one call site (:311, grep-confirmed); the numeric-receiver arm (:382) already de-edits the same source via its own AsNum, so no other path changes. Signed numeric-edited senders (e.g. PIC -ZZ9.99) now also preserve the sign through DeEdit (GR6d1 'may be signed') instead of losing it in NumFromAlphanumeric.
- **Golden (spec-derived):** IDENTIFICATION DIVISION.
PROGRAM-ID. CA27.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 NUM PIC 99V99 VALUE 12.34.
01 SRC PIC ZZ9.99.
01 DST PIC 999.99.
PROCEDURE DIVISION.
    MOVE NUM TO SRC.   *> SRC = edited image ' 12.34'
    MOVE SRC TO DST.   *> numeric-edited -> numeric-edited (under test)
    DISPLAY DST.
    STOP RUN.

SPEC-DERIVED EXPECTED (all editions 85/2002/2014/2023): DISPLAY prints '012.34'. Derivation: Table 16 permits the SRC→DST move; §14.9.25.4 GR5+GR6d1 de-edit SRC (' 12.34') to the numeric value 12.34 (scale 2); GR5/GR6 + §14.6.8 edit that value into DST's 999.99 mask, decimal-point aligned → integer '012', fraction '34' → '012.34'. CURRENT CODE prints '234.00' (NumFromAlphanumeric(' 12.34')=1234 at scale 0, edited into 999.99 → low-3 int digits '234', fraction '00'). Signed variant (declare SRC PIC -ZZ9.99, MOVE -12.34): spec de-edits to -12.34 and, into a sign-bearing DST, retains the sign; current code drops the sign (NumFromAlphanumeric is unsigned).

### CA28 · [MAJOR/S] · move-convert · ✅ LANDED (DEVLOG 998 — also retracted spec-wrong test + VCR 130c)
- **Spec:** §14.9.25.4 GR4 (group-move rule: 'no conversion of data from one form of internal representation to another') ; GR6/GR6a sign-drop is scoped to 'valid elementary moves' only
- **Verified:** SPEC: §14.9.25.4 GR4 first paragraph: a move is an ELEMENTARY move only when 'the sending operand is either a literal or an elementary item AND the receiving item is an elementary item'. With a GROUP receiver the move is NOT elementary, so GR4 second paragraph governs: it is 'treated exactly as if it were an alphanumeric to alphanumeric elementary move, EXCEPT THAT THERE IS NO CONVERSION OF DATA FROM ONE FORM OF INTERNAL REPRESENTATION TO ANOTHER ... the receiving area will be filled without consideration for the individual elementary or group items'. The sign-dropping rule lives in GR6a ('If the sending operand is described as being signed numeric, the operational sign is not moved'), which sits under GR6 ('conversion ... takes place during VALID ELEMENTARY MOVES'). A group move is not an elementary move, and dropping a trailing-overpunch sign ('12L'→'123') is precisely a conversion of internal representation, which GR4 forbids. So the sender's DISPLAY zoned/overpunch image must be copied verbatim. This is the classic 'group moves preserve the overpunch sign' behavior (GnuCOBOL/IBM/MicroFocus all agree). CODE: EmitGroupMove (the MoveKind.Group path — MoveClassifier.cs:47 routes any group RECEIVER here) renders the elementary source at MoveEmitter.cs:183 via `OperandText.AsString(source, num, deSign: true)`. deSign:true sends a signed numeric leaf through CobolNum.FormatUnsignedDisplay (OperandText.cs:98-99), STRIPPING the operational sign; for the default TrailingOverpunch it replaces the overpunched final digit with a plain digit. VERIFIED the greenfield's internal representation: CobolNum.cs:238-263 defines PositiveOverpunch="{ABCDEFGHI", NegativeOverpunch="}JKLMNOPQR", trailing by default, so -123 encodes to '12L' (NegativeOverpunch[3]='L') and +123 to '12C'. ASYMMETRY CONFIRMED: a GROUP sender into a group renders via OperandText.cs:83-86 `.AsImage()`, which (per the :74-82 comment) KEEPS the trailing-overpunch sign; deSign is ignored for a group. So two GR4 group moves that must behave identically diverge solely on whether the sender is elementary (sign dropped, wrong) or a group (sign kept, right). The justifying comment at MoveEmitter.cs:175-176 cites §8.8.4.1 — the RELATION-CONDITION rule (a group compares as alphanumeric, §8.8.4.2.5 does drop the numeric sign in comparison) — mis-applied to MOVE, where GR4 explicitly bars representation conversion.
- **Fix:** src/Cobol.Net.Compiler/CodeGen/Verbs/MoveEmitter.cs:183 — change `OperandText.AsString(source, num, deSign: true)` to `OperandText.AsString(source, num, deSign: false)` (i.e. drop the deSign override; default is false), and rewrite the misleading comment at :175-176 to cite §14.9.25.4 GR4 (group move = no internal-representation conversion; GR6a sign-drop applies only to valid elementary moves), not §8.8.4.1. MECHANISM: deSign:false renders a signed numeric elementary source through CobolNum.FormatDisplay (sign-aware, OperandText.cs:100) = its zoned/overpunch DISPLAY image '12L', which is then StrStore width-fitted and distributed to the group's leaves via FromImage, copying the representation verbatim as GR4 requires. This is a NO-OP for non-numeric and unsigned-numeric senders (FormatDisplay==FormatUnsignedDisplay for unsigned), and it aligns the elementary-sender path with the already-correct group-sender path (OperandText.cs:83-86). NOTE (out of scope for this defect): PICTURE-P scaling in a group-move source is a separate implementor-defined edge the deSign:false FormatDisplay path handles via NumProfile; CA28 concerns only the sign.
- **Golden (spec-derived):** IDENTIFICATION DIVISION.
PROGRAM-ID. CA28.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 A PIC S9(3) VALUE -123.
01 G.
   05 B PIC X(3).
PROCEDURE DIVISION.
    MOVE A TO G.
    DISPLAY B.
    STOP RUN.

SPEC-DERIVED EXPECTED (all editions 85/2002/2014/2023): DISPLAY prints '12L'. Derivation: receiver G is a group, so per §14.9.25.4 GR4 the move is NOT an elementary move and is done 'with no conversion of data from one form of internal representation to another'; the sign-drop of GR6a applies only to valid elementary moves. A's internal DISPLAY representation of -123 is the trailing-overpunch image '12L' (implementor-defined per §13.18.60 USAGE GR4; this compiler's documented table CobolNum.cs:238-240, NegativeOverpunch[3]='L'). Copied verbatim → B = '12L'. CURRENT CODE prints '123' (deSign:true → FormatUnsignedDisplay strips the overpunch). Positive check: VALUE +123 → spec '12C' (PositiveOverpunch[3]='C') vs code '123'. Cross-check for internal consistency: replacing A with a GROUP holding the same S9(3) leaf and MOVEing that group to G already yields '12L' today (OperandText.AsImage keeps the sign) — the elementary sender must match.

### CA29 · [MAJOR/S] · oo · ✅ LANDED (DEVLOG 1081)
- **Spec:** §14.8.2.3.3 rule 2a; §14.9.25.4 GR6d2b (spec lines 25084, 28622)
- **Verified:** SPEC: §14.8.2.3.3 rule 2a — for a numeric formal passed BY CONTENT/BY VALUE, 'the conformance rules are the same as for a COMPUTE statement with the argument as the sending operand and the corresponding formal parameter as the receiving operand.' The COMPUTE store into an unsigned receiver follows §14.9.25.4 GR6d2b: 'When an unsigned numeric item is the receiving item, the absolute value of the sending value is used, and no operational sign is generated for the receiving item.' So a signed value passed BY CONTENT into an unsigned numeric formal must be stored as its ABSOLUTE value. CODE: OoEmitter.cs:691-696 routes the COMPUTE-rule conversion (RuntimeApi.NumStore, which applies the abs rule — verified at CobolNum.cs:56-63 `return receiver.Signed ? v : Int128.Abs(v)`) ONLY when `cp.Item.Pic?.Digits != a.Formal.Pic!.Digits || cp.Item.Pic?.Scale != a.Formal.Pic.Scale`. When the argument and formal share digits AND scale and differ ONLY in signed-vs-unsigned, the guard is FALSE and control falls to the plain arm at :697-699 `{ElementType} tmp = ({ElementType})(Num.AsNum(source).Expr)`, which copies the raw signed native value with NO store/profile applied. ElementType for an unsigned PIC 9(n) fixed item is a SIGNED C# `long`/`Int128` (PicInfo.cs:294-304 — signedness is enforced only by the bypassed Store, not by the CLR type), so the negative value is copied verbatim. The abs-value rule is silently skipped. Note the numeric-RECEIVER MOVE path de-signs correctly, so the two paths disagree (the same asymmetry class the audit flags for CA27).
- **Fix:** src/Cobol.Net.Compiler/CodeGen/Verbs/OoEmitter.cs:693 — extend the conversion guard to also fire on a signedness difference: change `&& (cp.Item.Pic?.Digits != a.Formal.Pic!.Digits || cp.Item.Pic?.Scale != a.Formal.Pic.Scale))` to `&& (cp.Item.Pic?.Digits != a.Formal.Pic!.Digits || cp.Item.Pic?.Scale != a.Formal.Pic.Scale || cp.Item.Pic?.Signed != a.Formal.Pic!.Signed))`. This routes the sign-only-difference case through RuntimeApi.NumStore, which applies Int128.Abs for the unsigned formal (CobolNum.Store, verified). PicInfo exposes the `Signed` bool (PicInfo.cs:163). Mechanism: NumStore(cx.Expr, cx.Scale, qualProfile) stores through the OWNER's internal NumProfile whose Signed=false yields Int128.Abs. Cleaner alternative (note for owner): drop the digits/scale/sign guard entirely and ALWAYS route a BY CONTENT numeric→numeric argument through NumStore (§14.8.2.3.3 rule 2a is unconditional COMPUTE conformance; the guard is a value-preservation micro-opt that is only sound when the descriptions are identical). Follow-up (feedback_scan_all_similar): verify the BY VALUE numeric path and the CALL BY CONTENT path (CallEmitter) do not share the same guard.
- **Golden (spec-derived):** CLASS-ID C. FACTORY. ... END FACTORY. OBJECT. ... METHOD-ID M. DATA DIVISION. LINKAGE SECTION. 01 F PIC 9(4) COMP. PROCEDURE DIVISION USING BY REFERENCE F. IF F = 7 DISPLAY 'OK' ELSE DISPLAY 'BAD' END-IF. END METHOD M. END OBJECT. END CLASS C.  //  Main: 01 OBJ USAGE OBJECT REFERENCE C. 01 S PIC S9(4) COMP VALUE -7. ... INVOKE C 'NEW' RETURNING OBJ. INVOKE OBJ 'M' USING BY CONTENT S.  ||  SPEC-DERIVED EXPECTED: §14.8.2.3.3 r2a → COMPUTE conformance → §14.9.25.4 GR6d2b (unsigned receiver = |sending value|): F receives |−7| = 7, method prints 'OK'.  ACTUAL (bug): guard false (Digits 4=4, Scale 0=0), plain copy `long F = (long)(−7L)` = −7 → prints 'BAD'. (A POSITIVE VALUE +7 would print OK by accident; the divergence needs a negative signed argument.) Editions: 2002+ (INVOKE / OO).

### CA33 · [MAJOR/M] · picture-usage-value · ✅ LANDED (DEVLOG 1005)
- **Spec:** ISO §13.18.40.3 SR14 (numeric-edited digit-position cap 1–31) + §13.18.40.4 GR14 (Z/* are numeric positions; P is 'counted in the maximum number of digit positions'); edition cap 18(pre-2002)/31 via EditionContext.MaxDigits
- **Verified:** SPEC: §13.18.40.3 SR14 (ISO_COBOL.md:20058) — 'For data items of category numeric, and for fixed-point data items of category numeric-edited, the number of digit positions described by character-string-1 shall range from 1 through 31.' §13.18.40.4 GR14 (:20283) defines Z as 'a leading numeric position that during editing will contain a numeric character in the range 0 through 9' and * (:20309) identically — each is a digit-bearing position; and P (:20245) 'is not counted in the size of the item, but each symbol P is counted in the maximum number of digit positions.' So the SR14 count = 9 + Z + * + P + floating-run digit positions. CODE: PictureAnalyzer sets a numeric-edited item's Digits = `expanded.Count(c => c is '9')` (:219, used at :268) — the '9' count ONLY. DataBinder.cs:2065-2066 gates on `pic is { Category: Numeric or NumericEdited, IsFloat:false, Digits: > 0 }` then calls CheckDigitCapacity(pic.Digits,...). TWO failure modes: (a) mixed Z/9 undercounts — `Z(11)9(8)` has 19 digit positions but Digits=8, so no cap check fires; (b) all-suppression pictures have Digits=0, so the `Digits: > 0` guard skips the check entirely — `Z(35)` (35 positions) sails through. The analyzer even already computes `digitPositions = expanded.Count(c => c is '9' or 'Z' or '*')` at :233 (for P-scale classification) but never uses it for the cap. Pure-numeric-without-P is unaffected (Digits == digit positions), consistent with the finding's note that sibling `PIC 9(19)` IS rejected. This accepts over-capacity items ISO rejects (an edition/limit strictness gap; the item remains value-faithful so there is no wrong runtime value — lower impact than CA34/CA35).
- **Fix:** Compute the true digit-position count in PictureAnalyzer and use it at the cap gate. (A) In PictureAnalyzer.Analyze, after :233, compute `digitPos = expanded.Count(c => c is '9' or 'Z' or '*') + leadingP + trailingP + floatingExtra`, where floatingExtra sums (runLen-1) over each maximal run of ≥2 identical floating symbols (c is '+' , '-' , or c == cs) — the leftmost floating symbol is the sign/currency position, not a digit (§13.18.40.5 rule 6: 'The second floating symbol represents the leftmost limit of the numeric data'). NOTE the embedded-simple-insertion case (`$$,$$9`, editing-rule-6 'embedded simple insertion symbols are part of the string') needs the comma/period skipped when measuring a floating run for full correctness — a secondary refinement beyond the finding's failing cases. (B) Add an `int DigitPositions` property to PicInfo (Model/PicInfo.cs) and set it on the Numeric (:273) and NumericEdited (:267) return sites (for pure numeric DigitPositions == Digits + P; identical to today for P-free numerics, so no regression). (C) Change DataBinder.cs:2065-2066 to gate/call on `pic.DigitPositions`: `if (pic is { Category: PicCategory.Numeric or PicCategory.NumericEdited, IsFloat: false } && pic.DigitPositions > 0) Edition.CheckDigitCapacity(pic.DigitPositions, ...);`. CheckDigitCapacity (EditionContext.cs:114) already emits COBOLNET0801 (>31) / COBOLNET0802 (>18 pre-2002) — no new code. Mechanism: the cap is computed against the ISO digit-position count, so both undercount and the Digits=0 skip are eliminated.
- **Golden (spec-derived):** COBOL-85 mixed Z/9 over the 18-cap: `IDENTIFICATION DIVISION. PROGRAM-ID. CA33A. DATA DIVISION. WORKING-STORAGE SECTION. 01 A PIC Z(11)9(8). PROCEDURE DIVISION. DISPLAY 'X'. STOP RUN.` compiled `--std 85` — DERIVED from §13.18.40.3 SR14 + the COBOL-85 18-digit cap: 11 Z + 8 nine = 19 digit positions > 18 → spec-correct is REJECT COBOLNET0802 (the analyzer already rejects sibling `PIC 9(19)` at --std 85). Buggy actual: Digits=count('9')=8 ≤ 18 → accepted, no diagnostic. --- COBOL-2023 all-suppression over the 31-cap: `01 C PIC Z(35).` compiled `--std 2023` — DERIVED from SR14: 35 Z = 35 digit positions > 31 → spec-correct is REJECT COBOLNET0801. Buggy actual: Digits=count('9')=0 → the `Digits: > 0` guard skips CheckDigitCapacity → accepted. (Also `01 D PIC Z(20)9(12).` at --std 2023 = 32 > 31, wrongly accepted.)

### CA34 · [MAJOR/M] · picture-usage-value · ✅ LANDED (DEVLOG 1006)
- **Landed:** new `DataBinder.ValidateNumericValue` (called at the item-VALUE site for `Category: Numeric, IsFloat:
  false`). SR3 = a NEGATIVE literal into an unsigned subject → COBOLNET1625 (a leading '+' is the harmless idiom, not
  rejected). SR2 = model the stored ('9') digit positions by their power-of-ten exponents — uniformly (V / leading-P /
  trailing-P) the stored span is `[-Scale, Digits-Scale-1]`; the literal is representable iff every NONZERO digit lands
  in that span (else leading- or trailing-nonzero truncation). Value-zero and picture-less (INDEX) skipped. New diag
  **COBOLNET1625** (NOT 0803 — the whole 0801–0899 band is allocated; next-free per session-probe). Strictness sweep
  `wf_ebe10542-4e0`: 806 files, 0 genuine regressions (all 92 NIST P-scaled VALUEs fit once scale is applied). Gate:
  Conformance 3891/3891 · characterization 33/33 · drift 28/28. Goldens `negative/ca34_value_range_over` +
  `ca34_value_negative_unsigned` (edition-invariant).
- **Spec:** ISO §13.18.63.3 SR2 (numeric VALUE range/exact-representability) and SR3 (signed literal into unsigned item)
- **Verified:** SPEC: SR2 (§13.18.63.3, ISO_COBOL.md:22906) — 'If the category of the subject of the entry is numeric, all literals in the VALUE clause shall be numeric and shall be permissible values within the range indicated by the PICTURE clause... representable exactly in the subject of the entry, without truncation of leading or trailing nonzero digits' (the exceptions a/b are float-only). SR3 (:22914) — a signed numeric literal requires a signed-numeric (or sign-bearing numeric-edited) subject. Both are SYNTAX RULES ('shall'), so the only spec-conformant response to a violation is a compile-time diagnostic (a 'lenient truncating store' to 45/+5 is a legacy extension, NOT spec-conformant). CODE: for PicCategory.Numeric the initializer is ValueInitializer.cs:169 `EmitText.UnscaledAtScale(raw, pic.Scale)`. UnscaledAtScale (EmitCore.cs:142-167) only rescales the fractional part — it does NO high-order modulo (no `%= 10^Digits`) and NO unsigned magnitude (no Int128.Abs): for raw="12345", scale=0 it returns the literal `12345L`; for raw="-5" it returns `-5L`. This is assigned verbatim into the item's native long/Int128 field, bypassing the receiver Store normalization every runtime store applies. The only VALUE validator, DataBinder.ValidateValueCategory (:1096-1142), covers ONLY National/Boolean category+size and the cross-category N"…"/B"…" guard — I grepped the entire compiler for §13.18.63 SR2/SR3 / range / 'permissible value' checks and found NONE for category-numeric. So an out-of-range / wrong-sign numeric VALUE is neither rejected nor normalized; it silently seeds an out-of-range field, giving wrong comparisons at runtime. This is the highest-impact of the confirmed findings because it produces a silently-WRONG runtime value, not merely a missing strictness diagnostic.
- **Fix:** Add a numeric-VALUE validator in DataBinder, called alongside ValidateValueCategory at DataBinder.cs:2070: `if (rawValue is { } rvN && pic is { Category: PicCategory.Numeric, IsFloat: false }) ValidateNumericValue(pic, rvN, entryWhere);`. New method ValidateNumericValue(PicInfo pic, string raw, string where): (1) return if raw is not a plain numeric literal (figurative ZERO / quoted are handled elsewhere and are in-range or non-numeric-category). (2) Parse sign + integer-digit-run + fractional-digit-run (reuse the parse in ValueInitializer.TryParseNumeric to get unscaled+scale, or parse inline). (3) SR3: if the literal has a leading '-' and pic.Signed is false → Edition.Error(COBOLNET0803, "a signed numeric literal VALUE requires a signed numeric or sign-bearing numeric-edited item (ISO §13.18.63.3 SR3)"). (Scope to a negative sign to avoid false-rejecting the `VALUE +5`-into-signed idiom; note the strict reading treats explicit '+' as signed too.) (4) SR2: derive item integer positions and fractional positions from pic — for Scale>=0: intPos = pic.Digits - pic.Scale, fracPos = pic.Scale; for Scale<0 (trailing P): intPos = pic.Digits, fracPos = 0 and any nonzero literal digit falling in the low |Scale| assumed-zero positions is truncation. If significant integer digits > intPos (leading-nonzero truncation) OR nonzero fractional digits fall beyond fracPos (trailing-nonzero truncation) → Edition.Error(COBOLNET0803, "the VALUE literal is not a permissible value representable in the item's PICTURE range without truncation (ISO §13.18.63.3 SR2)"). Register a new descriptor COBOLNET0803 'value-numeric-out-of-range' (Error, ISO §13.18.63.3 SR2/SR3) in DiagnosticCatalog.cs value/picture band (0803-0807 are unallocated; confirm via the next-free scan). Mechanism: the check runs at bind time on the constant literal, so a nonconforming VALUE fails loud (owner doctrine: reject illegal source, never silently mis-store) rather than seeding an out-of-range native field.
- **Golden (spec-derived):** SR2 (range/truncation): `IDENTIFICATION DIVISION. PROGRAM-ID. CA34A. DATA DIVISION. WORKING-STORAGE SECTION. 01 A PIC 99 VALUE 12345. PROCEDURE DIVISION. STOP RUN.` — DERIVED from §13.18.63.3 SR2: 12345 needs 5 integer digit positions but PIC 99 has 2 → truncation of the leading nonzero digits 1,2,3 → SR2 violated → spec-correct result is a COMPILE-TIME REJECTION (COBOLNET0803). Buggy actual: no diagnostic; A initialized to `12345L` (UnscaledAtScale('12345',0) with no modulo), so `IF A = 12345` is TRUE and `IF A = 45` is FALSE. --- SR3 (sign): `01 U PIC 99 VALUE -5.` — DERIVED from §13.18.63.3 SR3: a signed literal (-5) requires a signed-numeric subject; PIC 99 is unsigned → SR3 violated → spec-correct is a COMPILE-TIME REJECTION. Buggy actual: no diagnostic; U = `-5L` (a negative in an unsigned field), so with `01 W PIC 99. MOVE -5 TO W.` (which correctly yields +5) `IF U = W` is FALSE. Editions: SR2/SR3 are edition-invariant (present 85/2002/2014/2023).

### CA35 · [MAJOR/S] · picture-usage-value · ✅ LANDED (DEVLOG 1007)
- **Landed:** a one-branch SR3 guard in `PictureAnalyzer.Analyze` immediately before the `anyAlpha` return, mirroring
  the BIT SR5 / NATIONAL SR12 guards — `if (anyAlpha && usage is Usage.Binary or Usage.Comp5 or Usage.Packed)` →
  COBOLNET0881 (reused; no new descriptor) + recover to Display. Message names the COBOL keyword (BINARY /
  PACKED-DECIMAL / COMPUTATIONAL-5). `PIC XX COMP` was silently binding as Alphanumeric with the usage dropped; now
  rejected. Corpus grep = 0 genuine at-risk cases. Gate: corpus runner 334/334 · characterization 33/33 · full
  Conformance. Goldens `negative/ca35_comp_alpha_picture` + `ca35_packed_alpha_picture` (edition-invariant).
- **Spec:** ISO §13.18.60.3 SR3 (BINARY/COMPUTATIONAL/PACKED-DECIMAL require a numeric picture)
- **Verified:** SPEC: §13.18.60.3 SR3 (ISO_COBOL.md:22438) — 'An elementary data item whose declaration contains ... a USAGE clause specifying BINARY, COMPUTATIONAL, or PACKED-DECIMAL shall be specified only with a picture character-string that describes a numeric item.' A 'shall' syntax rule ⇒ a non-numeric picture with such a usage MUST be diagnosed. CODE: PictureAnalyzer.Analyze handles the BIT case (usage BIT with a non-boolean picture → COBOLNET0881 hard error at :193-196, per SR5) and NATIONAL (:199-214, per SR12) BEFORE the category dispatch, but there is NO analogous guard for Usage.Binary/Comp/Packed. When the picture contains X or A (`anyAlpha`, :245), the method returns `new PicInfo(PicCategory.Alphanumeric, usage, ... Digits:0 ...)` (:251-255) regardless of a BINARY/COMP/PACKED usage — no diagnostic. I grepped the binder for a §13.18.60.3 SR3 / 'describes a numeric' guard and found none, so nothing downstream catches it either. The item silently binds as PicCategory.Alphanumeric (string storage) and the numeric usage is effectively discarded — exactly the silent-misbind the sibling BIT/NATIONAL checks are written to prevent. This accepts a program ISO rejects and drops a declared usage.
- **Fix:** In PictureAnalyzer.cs, add an SR3 guard mirroring the BIT SR5 branch (:193-198). Immediately before the `if (anyAlpha)` return at :245 (where `anyAlpha` is known), add: `if (anyAlpha && usage is Usage.Binary or Usage.Comp5 or Usage.Packed) { edition.Error("COBOLNET0881", $"{where}: USAGE {usage} requires a PICTURE that describes a numeric item — PICTURE {picture} is alphabetic/alphanumeric (ISO §13.18.60.3 SR3)"); usage = Usage.Display; }` (recover to Display, matching the BIT/NATIONAL recovery so the doomed emit stays crash-free). Scope to the usages that take an explicit PICTURE and are named by SR3 (Binary=COMP/BINARY, Packed=PACKED-DECIMAL/COMP-3, Comp5=COMPUTATIONAL-5); BINARY-CHAR/-SHORT/-LONG/-DOUBLE are picture-less and a picture with them is a distinct error handled elsewhere. Reuse COBOLNET0881 (the established usage/picture-mismatch code the BIT and NATIONAL SR checks already emit) — no new descriptor needed. Mechanism: the guard fires at picture analysis, the single choke point for usage↔picture legality, so all three SR3 shapes are rejected loud instead of misbinding as alphanumeric.
- **Golden (spec-derived):** `IDENTIFICATION DIVISION. PROGRAM-ID. CA35. DATA DIVISION. WORKING-STORAGE SECTION. 01 A PIC XX COMP. PROCEDURE DIVISION. STOP RUN.` — DERIVED from §13.18.60.3 SR3: PIC XX describes an alphanumeric (non-numeric) item, so the COMP usage is illegal → spec-correct result is a COMPILE-TIME REJECTION (COBOLNET0881, exactly as the enforced USAGE BIT SR5 case rejects `PIC XX BIT`). Buggy actual: compiles with no diagnostic; A binds as PicCategory.Alphanumeric → `string A = new string(' ', 2)` in the generated C#, usage silently dropped. Same for `01 B PIC XXX USAGE PACKED-DECIMAL.` and `01 C PIC A(4) BINARY.` Editions: edition-invariant (SR3 present 85/2002/2014/2023).

### CA36 · [MAJOR/M] · tables-refmod · ✅ LANDED (DEVLOG 1012)
- **Landed:** `EmitSearchScan` now DISPATCHES a raised range EC when AT END is absent and EC-RANGE checking is on
  (§14.9.37.4 GR1b2), mirroring `EcEmitter.EmitOverflow`. A new `ControlFlowEmitter.Ec` property (wired by
  `UnitEmitters` like `Statements`); gated on `dispatchEc = s.AtEnd is null && (CheckSearchIndex || CheckSearchNoMatch)`,
  a `__searchEc{id}` var tracks which EC was raised (set by a `RaiseRange` helper at the three raise sites), and the
  shared `__searchAtEnd` funnel emits `int __searchR{id}=EcDispatchExpr(...); if(>=0){__pc=..;break;}` before falling
  through to `__searchEnd`. Purely additive to that niche — characterization 33/33 byte-identical. Golden
  `2002/ca36_search_range_dispatch` (DECL-RAN / AFTER-SEARCH). Gate: Corpus+Search+Range+Exception 440/440 · full Conformance.
- **Spec:** §14.9.37.4 GR1b2 (raise sites GR4/GR6/GR9); Table 13 (EC-RANGE-SEARCH-INDEX / -NO-MATCH = Nonfatal); §14.6.13.1.4 #3
- **Verified:** SPEC: §14.9.37.4 GR1b2 (spec line 30520, quoted verbatim): 'If the AT END phrase is not specified and either the EC-RANGE-SEARCH-INDEX or EC-RANGE-SEARCH-NO-MATCH exception condition was raised during the execution of the SEARCH statement and an applicable exception processing statement associated with that exception condition exists, control is transferred according to the rules for that statement, and if control is returned from that statement, control is transferred to the end of the SEARCH statement.' So when checking is enabled (EC raised) and AT END is absent, an applicable USE-AFTER-EXCEPTION declarative (or Format-3 PERFORM WHEN) MUST be selected and executed. CODE: EmitSearchScan (ControlFlowEmitter.cs:375-415) has exactly three unsuccessful-path emissions — the initial-index guard (SEARCH ALL empty-table NO-MATCH at :388; serial out-of-range INDEX at :390-391) and the advance-past-end NO-MATCH at :406 — each of which only emits `ExceptionState.Set("EC-RANGE-SEARCH-...", false)` then `goto __searchAtEnd{id}`. The shared __searchAtEnd funnel (:411-413) runs the AT END statements only when s.AtEnd is non-null; when AtEnd is absent it emits `goto __searchEnd{id}` and nothing else. There is NO EcDispatchExpr / __EcDispatch / __EcPerform call anywhere on the SEARCH path (grep of the file confirms). ExceptionState.Set records only the last-exception status; it does not dispatch to declaratives (dispatch is a separate step everywhere else — see EcEmitter.EmitOverflow:233-234, EmitRaise:172-173, EmitArgOrPlain:140). Verified contrast: EmitOverflow (EcEmitter.cs:222-237) for the analogous 'exception-phrase absent' case DOES emit `int __r = EcDispatchExpr(...); if (__r >= 0) { __pc = __r; break; }`. SEARCH is missing this. Result: with `>>TURN EC-RANGE CHECKING ON`, a SEARCH with a matching USE declarative but no AT END phrase silently skips the declarative on an unsuccessful search — a mandated handler is never run. CONFIRMED.
- **Fix:** src/Cobol.Net.Compiler/CodeGen/Verbs/ControlFlowEmitter.cs, EmitSearchScan (375-415). Mechanism: mirror EcEmitter.EmitOverflow's no-phrase dispatch on the shared AT-END funnel, gated on `s.AtEnd is null`. (1) Give ControlFlowEmitter access to EcEmitter.EcDispatchExpr — add an `internal EcEmitter Ec { get; set; }` property property-wired by UnitEmitters exactly like the existing `Statements` property (same cyclic-edge rationale). (2) Track which range EC was raised: when `s.AtEnd is null && (s.CheckSearchIndex || s.CheckSearchNoMatch)`, declare `string {ec}=null;` (`ec=$"__searchEc{id}"`) before the init guard (before :384); at each raise site set it alongside the existing Set call — `{ec} = "EC-RANGE-SEARCH-INDEX";` at :391 and `{ec} = "EC-RANGE-SEARCH-NO-MATCH";` at :388 and :406. (3) In the __searchAtEnd block (:411-413), when `s.AtEnd is null`, before the `goto __searchEnd{id}` emit: `if ({ec} != null) { int {r} = {Ec.EcDispatchExpr(ec, "\"\"")}; if ({r} >= 0) { __pc = {r}; break; } }` then fall through to `goto __searchEnd{id}`. `>= 0` is RESUME AT procedure-name (transfer via the dispatcher `break`); -1 (declarative ran)/-2 (RESUME NEXT)/-3 (no handler) all fall through to __searchEnd = 'the end of the SEARCH statement' per GR1b2 / §14.6.13.1.4 #3-#4 (nonfatal — never rethrows). The change is purely additive to the `AtEnd==null && checking-on` niche, so all other SEARCH output is byte-identical.
- **Golden (spec-derived):** Editions: 2002+ (SEARCH/OCCURS date to 1985 but EC-RANGE checking + USE AFTER EXCEPTION CONDITION are the 2002 exception model). Program:
>>TURN EC-RANGE CHECKING ON
 IDENTIFICATION DIVISION.
 PROGRAM-ID. CA36.
 DATA DIVISION.
 WORKING-STORAGE SECTION.
 01 T.
    05 E PIC 9 OCCURS 3 INDEXED BY IX.
 PROCEDURE DIVISION.
 DECLARATIVES.
 H-SEC SECTION.
     USE AFTER EXCEPTION CONDITION EC-RANGE-SEARCH-NO-MATCH.
 H-PARA.
     DISPLAY "DECL-RAN".
 END DECLARATIVES.
 M-SEC SECTION.
 M-PARA.
     MOVE 1 TO E(1)
     MOVE 2 TO E(2)
     MOVE 3 TO E(3)
     SET IX TO 1
     SEARCH E WHEN E(IX) = 9 CONTINUE END-SEARCH
     DISPLAY "AFTER-SEARCH"
     STOP RUN.
SPEC-DERIVED EXPECTED OUTPUT:
DECL-RAN
AFTER-SEARCH
Derivation: no element equals 9, so per §14.9.37.4 GR4 the scan advances past occurrence 3 and 'the search operation is unsuccessful, the EC-RANGE-SEARCH-NO-MATCH exception condition is set to exist' (checking enabled by the TURN of the level-2 parent EC-RANGE). AT END is absent → GR1b2: an applicable exception-processing statement (the USE AFTER EXCEPTION CONDITION EC-RANGE-SEARCH-NO-MATCH declarative) exists → control transfers to it → 'DECL-RAN' printed. EC-RANGE-SEARCH-NO-MATCH is Nonfatal (Table 13), the declarative returns normally, so 'control is transferred to the end of the SEARCH statement' → execution continues → 'AFTER-SEARCH' printed; exit 0. CURRENT CODE prints only 'AFTER-SEARCH' ('DECL-RAN' missing) because the status is set but never dispatched.

### CA39 · [MAJOR/M] · editions-gating · ✅ LANDED (DEVLOG 999)
- **Spec:** §14.9.14.2 Formats 3 (EXIT PERFORM [CYCLE]) & 4 (EXIT PARAGRAPH / EXIT SECTION); §14.9.14.3 SR1 (Format-1 85 EXIT) / SR8; §14.9.14.4 GR5a & GR6 (requested EXIT PARAGRAPH / EXIT PERFORM cross-check)
- **Verified:** Independently verified per the cross-check directive. Spec (specs/ISO_COBOL.md:27028-27046): EXIT has Format 1 (bare EXIT — must be the only sentence in a paragraph/section, §14.9.14.3 SR1 at :27061 — the COBOL-85 form), Format 2 EXIT PROGRAM (85; archaic 2023), Format 3 EXIT PERFORM [CYCLE], Format 4 EXIT PARAGRAPH / EXIT SECTION. Formats 3 and 4 are the structured-procedure exits introduced together in COBOL-2002 (the inline-PERFORM / structured-programming additions); in 85 the ONLY EXIT forms were bare EXIT and EXIT PROGRAM. The repo's OWN model already commits the siblings to 2002: constructs.json exit-section-2002 (introducedIn 2002, :28-38), exit-method-window / exit-function-window (2002->2023). EXIT PARAGRAPH is the Format-4 twin of EXIT SECTION; EXIT PERFORM/CYCLE is Format 3 — same 2002 vintage. CODE: VersionConformancePass.cs VisitExitStatement (:634-644) gates ctx.METHOD/FUNCTION/PROGRAM/SECTION only — NO arm for ctx.PARAGRAPH() or ctx.PERFORM(). The greenfield binder ControlFlowBinder.cs:85 ('if (e.PARAGRAPH() is not null) return new BoundExitParagraph(...)') and :86 ('if (e.PERFORM() is not null) return new BoundExitPerform(e.CYCLE() is not null)') bind both with no edition gate, and StatementEmitter.cs Visit(BoundExitParagraph)/Visit(BoundExitPerform) emit them. constructs.json has NO exit-paragraph-2002 or exit-perform-2002 row (grep: only exit-section-2002). Inline PERFORM is itself ungated in the repo (no inline-perform row; VisitPerform gates only PERFORM UNTIL EXIT/exception-checking, both 2023), so EXIT PERFORM is fully reachable at --std 85. Confirmed divergence: at --std 85 (default) both 'EXIT PARAGRAPH' and 'EXIT PERFORM'/'EXIT PERFORM CYCLE' compile clean with no diagnostic — a missing 2002 introduction gate exactly parallel to the EXIT SECTION gate V5 added. EXIT PARAGRAPH is the cleanest witness (standalone, no enclosing-construct dependency).
- **Fix:** Mirror the exit-section-2002 pattern. (1) tests/version-matrix/constructs.json — add two rows modeled on exit-section-2002 (:28-38): 'exit-paragraph-2002' (display 'the EXIT PARAGRAPH statement', citation 'ISO §14.9.14.2 Format 4 / §14.9.14.4 GR6', introducedIn 2002, removedIn null, diagnosticCode COBOLNET0900) and 'exit-perform-2002' (display 'the EXIT PERFORM statement', citation 'ISO §14.9.14.2 Format 3 / §14.9.14.4 GR5a', introducedIn 2002, removedIn null, diagnosticCode COBOLNET0900), each with a source that exercises it. Re-run scripts/gen-constructs.ps1 -> generates Constructs.g.cs constants ExitParagraph2002 / ExitPerform2002 and ConstructRegistry.g.cs rows. (2) src/Cobol.Net.Compiler/Validation/VersionConformancePass.cs VisitExitStatement — append after the SECTION arm (:643): 'else if (ctx.PARAGRAPH() is not null) _p.Check(Constructs.ExitParagraph2002, "the EXIT PARAGRAPH statement");' and 'else if (ctx.PERFORM() is not null) _p.Check(Constructs.ExitPerform2002, "the EXIT PERFORM statement");'. The phrase terminals are mutually exclusive on one ExitStatementContext, so appending two else-if arms is safe; EXIT PERFORM CYCLE rides the same PERFORM arm (CYCLE is also 2002, §14.9.14.3 SR8 confines it to inline PERFORM — no separate gate). Recognition-based, so it fires even when the construct also fails to bind, per the pass's existing doctrine.
- **Golden (spec-derived):** GOLDEN A (EXIT PARAGRAPH): 'IDENTIFICATION DIVISION.\nPROGRAM-ID. EXPARA.\nPROCEDURE DIVISION.\nP1.\n    EXIT PARAGRAPH.\n    DISPLAY "AFTER".\nP2.\n    DISPLAY "P2".\n    STOP RUN.' — rule §14.9.14.4 GR6 (control passes to the implicit CONTINUE at the end of P1, skipping DISPLAY "AFTER", falls into P2). At --std 2002+ accepted -> prints 'P2'. At --std 85 (default) EXPECTED = REJECT COBOLNET0900 'the EXIT PARAGRAPH statement requires COBOL-2002 (targeting COBOL-85)' (rule: §14.9.14.2 Format 4 is a 2002 introduction; the 85 EXIT is only Format 1, §14.9.14.3 SR1); CURRENT code accepts and prints 'P2' (the bug). GOLDEN B (EXIT PERFORM): 'IDENTIFICATION DIVISION.\nPROGRAM-ID. EXPERF.\nDATA DIVISION.\nWORKING-STORAGE SECTION.\n01 N PIC 9 VALUE 0.\nPROCEDURE DIVISION.\nMAIN.\n    PERFORM UNTIL N > 5\n        ADD 1 TO N\n        IF N = 3 EXIT PERFORM END-IF\n    END-PERFORM.\n    DISPLAY N.\n    STOP RUN.' — rule §14.9.14.4 GR5a (EXIT PERFORM at N=3 leaves the inline PERFORM). At --std 2002+ accepted -> prints '3'. At --std 85 (default) EXPECTED = REJECT COBOLNET0900 'the EXIT PERFORM statement requires COBOL-2002' (Format 3 is 2002; note the enclosing inline PERFORM is also a 2002 form and is itself ungated — a related separate gap); CURRENT code accepts and prints '3' (the bug). Editions affected: introduction boundary 2002 — reject at 85 for both.

### CA4 · [MAJOR/S] · arithmetic · ✅ LANDED (DEVLOG 1008)
- **Landed:** pass `[]` (no receivers) to `CheckComposite` at the ADD GIVING (ArithmeticBinder.cs:36) and SUBTRACT
  GIVING (:57) call sites — the resultants are excluded from the §14.7.7-rule-2 composite; the receiver lists still
  build the bound nodes (codegen identical). MULTIPLY/DIVIDE correctly untouched (§14.9.26.3 SR4 counts the GIVING
  receiver; DIVIDE already omits only the REMAINDER). Leniency change (accepts a legal program the code wrongly
  rejected) — the one COBOLNET0805 test (`StandardDecimalTests`) is operand-driven (40-digit) and unaffected. Golden
  `conformance/2023/ca4_giving_composite` (positive, .out-verified: 0000030000000000 ×2). Gate: Conformance
  Corpus+StandardDecimal+Arithmetic+Compute 400/400 · characterization 33/33.
- **Spec:** §14.9.2.3 SR1b (ADD Format 2) gives the composite verbatim — "all of the operands in the statement excluding the data items that follow the word GIVING" — and §14.9.44.3 SR1b (SUBTRACT Format 2) states the same rule, differing only in reading "the data item that follow" (singular); §14.7.7 rule 2 (the composite is the superimposition of the operands). Code: ArithmeticBinder.cs:35-37 (ADD GIVING) and :56-57 (SUBTRACT GIVING); StatementValidation.cs:105-143 (CheckComposite), :129-131 (receiver shaping).
- **Verified:** CONFIRMED. §14.9.2.3 SR1b and §14.9.44.3 SR1b (spec lines 25530 / 32133) both exclude the GIVING data items from the ADD/SUBTRACT Format-2 composite. ArithmeticBinder.BindAdd (:36) passes `givingRecv` and BindSubtract (:57) passes `recv` as the `receivers` argument to CheckComposite, which unconditionally Shape()s every numeric non-float receiver into maxInt/maxFrac (StatementValidation.cs:129-131). So the GIVING resultant's digit positions are wrongly counted against the 31-digit cap, and a conformant program is rejected with COBOLNET0805. I verified the scope is exactly ADD+SUBTRACT: MULTIPLY (§14.9.26.3 SR4, spec line 28770) says "all of the operands" with NO GIVING exclusion, and DIVIDE (§14.9.12.3 SR4, spec line 26731) excludes ONLY the REMAINDER data item — the code correctly includes the MULTIPLY/DIVIDE GIVING receivers and correctly omits the DIVIDE remainder (ArithmeticBinder.cs:79,108,118,129), so those verbs must NOT be changed. The check is edition-invariant (lives in StatementValidation; the GIVING-exclusion is present in all editions).
- **Fix:** ArithmeticBinder.cs: in the ADD GIVING branch change line 36 `ctx.Validation.CheckComposite("ADD", addends, givingRecv);` to `ctx.Validation.CheckComposite("ADD", addends, []);`, and in the SUBTRACT GIVING branch change line 57 `ctx.Validation.CheckComposite("SUBTRACT", [.. minuends, fromX], recv);` to `ctx.Validation.CheckComposite("SUBTRACT", [.. minuends, fromX], []);` — i.e. pass an empty IReadOnlyList<Receiver> so the GIVING resultants are excluded from the composite per SR1b (the receivers are still used unchanged to build BoundAddGiving/BoundSubtractGiving). `[]` target-types to the IEnumerable<Receiver> parameter. (Equivalent, more self-documenting alternative: add an overload CheckComposite(verb, operands) that omits the receiver loop and call it from the two GIVING branches.) Leave MULTIPLY/DIVIDE call sites untouched — they conform to §14.9.26.3 SR4 / §14.9.12.3 SR4.
- **Golden (spec-derived):** COBOL (any edition — 9(25) and the composite rule are edition-invariant):\nIDENTIFICATION DIVISION.\nPROGRAM-ID. CA4ADD.\nDATA DIVISION.\nWORKING-STORAGE SECTION.\n01 A PIC 9(25) VALUE 1.\n01 B PIC 9(25) VALUE 2.\n01 C PIC 9(6)V9(10).\nPROCEDURE DIVISION.\n    ADD A B GIVING C.\n    DISPLAY C.\n    STOP RUN.\nSPEC-DERIVED (§14.9.2.3 SR1b + §14.7.7 rule 2): composite excludes C = {A(25 int), B(25 int)} -> max int 25, max frac 0 -> composite 25 <= 31 -> LEGAL, compiles with no COBOLNET0805. Runtime A+B = 3 -> C (PIC 9(6)V9(10)) = 3.0. EXPECTED OUTPUT (DISPLAY of the 16 implied digits, V not shown):\n0000030000000000\nCURRENT (bug): CheckComposite also shapes C (6 int, 10 frac) -> max int 25, max frac 10 -> composite 35 > 31 -> emits COBOLNET0805 "the composite of operands spans 35 digits" and REJECTS the program.\nSUBTRACT analogue (§14.9.44.3 SR1b): with A PIC 9(25) VALUE 5, B PIC 9(25) VALUE 2, C PIC 9(6)V9(10), `SUBTRACT B FROM A GIVING C.` -> composite excludes C = {A,B} = 25 <= 31 -> legal, C = 3 -> 0000030000000000; current code rejects identically.

### CA5 · [MAJOR/M] · arithmetic · ✅ LANDED (DEVLOG 1009)
- **Landed:** an explicit `_outermost` flag in `NumericRenderer` replaces the wrong `ds==_rcv.Scale` proxy in
  `Divide` — threaded from the emit sites that own the final transfer (single-receiver COMPUTE render + DIVIDE
  top-level division = `outermost:true`; multi-receiver COMPUTE + DIVIDE operands = false). Outermost → compute at the
  resultant scale + ROUNDED mode (one exact step); nested → ALWAYS D2 guard scale + Truncation (never inherits the
  receiver's mode). Fixes both directions: (a) a nested integer-operand division no longer inherits PROHIBITED (no
  spurious size error) and (b) a multi-receiver root division no longer misses PROHIBITED. Goldens
  `2014/ca5a_prohibited_nested_no_size_error` (OK/0000) + `2014/ca5b_prohibited_multi_receiver_size_error`
  (SIZE-ERROR/0000/0000). ⚠ The finding's Program B was mis-derived (it assumed `COMPUTE X Y ROUNDED` rounds BOTH — but
  §14.9.8 ROUNDED is per-resultant, so only Y); the golden was re-derived with an explicit PROHIBITED phrase on each
  resultant. Gate: characterization 33/33 byte-identical · unit 575/575 · Corpus+arithmetic 430/430 · full Conformance.
- **Spec:** §14.7.4.3 GR7 (PROHIBITED tests "the resultant identifier"); §14.7.7 rule 3 NOTE 1 (ROUNDED applies only to the final transfer); §14.7.5 (size-error cases — no native intermediate-inexactness case); §8.8.1.3 (native intermediate = implementor-defined); Annex F change item 10 (spec line 49691: EC-SIZE-TRUNCATION arises only from PROHIBITED). Code: NumericRenderer.cs:267-285 (Divide), CobolNum.cs:184-204 (DivideOrThrow), ArithmeticEmitter.cs:165-173 (COMPUTE render), CobolNum.cs:120-136 (TryStore).
- **Verified:** CONFIRMED, and it is a two-directional defect with a single root cause. ROOT CAUSE: Divide() uses `mode = ds == _rcv.Scale ? _rcv.Rounding : Truncation` (NumericRenderer.cs:280) as a proxy for "this division's quotient is the value transferred to the resultant identifier." That proxy is wrong. (a) SINGLE-RECEIVER COMPUTE renders the whole RHS with the receiver's real mode: ArithmeticEmitter.cs:173 passes `RcvFor(r,ise)` whose Rounding = r.Rounding (= PROHIBITED). For a NESTED division whose operands' scales <= receiver scale (e.g. integer operands), the guard-digit condition at :271 is false, so ds = _rcv.Scale and mode inherits PROHIBITED; under InSizeError the emitted `CobolNum.DivideOrThrow(...,Prohibited)` throws CobolSizeError on the inexact intermediate quotient (verified: DivisionLosesPrecision returns true) — a SPURIOUS size error on a statement whose FINAL value is exactly representable. PROHIBITED per §14.7.4.3 GR7 tests only the resultant identifier, and §14.7.7 rule-3 NOTE 1 confines ROUNDED to the final transfer; native intermediate precision is implementor-defined (§8.8.1.3) and §14.7.5 enumerates NO intermediate-inexactness size-error case for native arithmetic (Annex F item 10 makes EC-SIZE-TRUNCATION exclusively a PROHIBITED-at-resultant condition). (b) During verification I found the MIRROR bug: the MULTI-receiver path (ArithmeticEmitter.cs:166-167) renders the RHS at Truncation and pre-truncates a ROOT division to the widest receiver scale, so a genuine PROHIBITED violation on the outermost division is MISSED — the per-store TryStore (CobolNum.cs:124) then rescales scale-N -> scale-N (identity), sees no inexactness, and silently stores a truncated value where §14.7.4.3 GR7 requires EC-SIZE-TRUNCATION + receiver-unchanged. The single-receiver ROOT PROHIBITED case is currently CORRECT (Divide computes at receiver mode, DivideOrThrow detects inexactness via the exact remainder) and the fix must preserve it. NOTE on the auditor's own example `10/3*3`: its "no size error" outcome is intermediate-precision-SENSITIVE (with guard digits 10/3*3 -> ~9.9999999999 is itself inexact at scale 2), so I derived a precision-INDEPENDENT golden instead. Native-arithmetic only (StandardDecimal short-circuits at CombineCore:210 to CobolDec).
- **Fix:** NumericRenderer.cs: replace the `ds == _rcv.Scale` heuristic with an explicit `_outermost` flag that marks a division whose quotient is transferred directly to ONE resultant identifier with that identifier's mode. (1) add `private bool _outermost;` near :43; save/restore it in Render (:53-61) and AsNum (:66-74) exactly like `_rcv`, add a `bool outermost=false` parameter to Render, and set `_outermost=false` at Fold entry (:181-188). (2) Rewrite Visit(BoundBinary) (:93) to `{ bool outer=_outermost; _outermost=false; var l=n.Left.Accept(this); var r=n.Right.Accept(this); _outermost=outer; return CombineCore(l,n.Op.ToString(),r); }` so children are never outermost while the node's own combine sees the entry value. (3) Add `bool outermost=false` to Combine (:195-199) and set `_outermost=outermost`. (4) Rewrite Divide (:267-285): if `_outermost` then `ds=_rcv.Scale; mode=_rcv.Rounding;` (compute at the resultant scale+mode — DivideOrThrow detects PROHIBITED via the exact remainder); else `ds = baseScale + Math.Max(0, guard)` computed ALWAYS (drop the `if (baseScale!=_rcv.Scale||...)` gate so a nested division at receiver scale still carries D2 guard digits) and `mode = CobolRounding.Truncation` (a nested quotient never inherits the receiver's mode; DivideOrThrow then only fires on the zero-divisor size error, §14.7.5 case 2). ArithmeticEmitter.cs: pass `outermost: true` at the single-receiver COMPUTE render (:173) and at the DIVIDE top-level division (:86, num.Combine(q,"/",...)); pass `outermost: false` (default) at the multi-receiver COMPUTE render (:167) and the DIVIDE operand renders (:75-76) — this makes the multi-receiver shared intermediate carry full guard precision so each per-store PROHIBITED (CobolNum.cs:124 TryStore) correctly tests exactness at its own receiver scale, fixing the mirror bug. VALIDATION NOTE: making nested divisions always carry guard digits + truncation changes some intermediate-rounding results toward more-accurate (and equally-conformant, more design-aligned) values; affected goldens must be re-derived from the spec, never copied from the legacy oracle.
- **Golden (spec-derived):** Program A (single-receiver spurious size error), COBOL-2014+ (ROUNDED MODE IS PROHIBITED is a 2014 feature; native default):\nIDENTIFICATION DIVISION.\nPROGRAM-ID. CA5A.\nDATA DIVISION.\nWORKING-STORAGE SECTION.\n01 X PIC 99V99 VALUE 7.\nPROCEDURE DIVISION.\n    COMPUTE X ROUNDED MODE IS PROHIBITED = (1 / 3) * 0\n        ON SIZE ERROR DISPLAY "SIZE-ERROR"\n        NOT ON SIZE ERROR DISPLAY "OK".\n    DISPLAY X.\n    STOP RUN.\nSPEC-DERIVED (§14.7.4.3 GR7 + §14.7.7 NOTE 1): the arithmetic value of (1/3)*0 = 0 for ANY implementor-defined intermediate precision; 0 is exactly representable in X (PIC 99V99) so PROHIBITED does NOT raise -> NOT ON SIZE ERROR runs; X stored 0. EXPECTED OUTPUT:\nOK\n0000\nCURRENT (bug): nested 1/3 emits CobolNum.DivideOrThrow(1,0,3,0,2,Prohibited) which throws (1/3 inexact at scale 2) -> ON SIZE ERROR runs; X unchanged (7). ACTUAL:\nSIZE-ERROR\n0700\n\nProgram B (mirror bug — multi-receiver MISSED PROHIBITED):\n01 X PIC 99V99 VALUE 0.\n01 Y PIC 99V99 VALUE 0.\n... COMPUTE X Y ROUNDED MODE IS PROHIBITED = 10 / 3\n        ON SIZE ERROR DISPLAY "SIZE-ERROR"\n        NOT ON SIZE ERROR DISPLAY "OK".\n    DISPLAY X.\nSPEC-DERIVED: 10/3 = 3.333... is inexact at X/Y scale 2 -> PROHIBITED sets EC-SIZE-TRUNCATION, X and Y unchanged. EXPECTED:\nSIZE-ERROR\n0000\nCURRENT (bug): RHS rendered at Truncation -> 3.33 (scale 2), per-store PROHIBITED sees no inexactness -> no size error; X=3.33. ACTUAL:\nOK\n0333

### CA6 · [MAJOR/S] · arithmetic · ✅ LANDED (DEVLOG 1010)
- **Landed:** a `static bool InComposite(PicInfo p)` predicate in `CheckComposite` (`{ Category: Numeric, IsFloat:
  false }` AND `Usage is not (BinaryChar/-Short/-Long/-Double)`) at both Shape sites — the four fixed-width binary
  usages are excluded from the §14.7.7-rule-2b composite (COMP-5 stays counted; floats already excluded by IsFloat).
  Pure-leniency (the composite only shrinks). Golden `2002/ca6_binary_operand_composite` (BINARY-DOUBLE; →
  000000000080000000000000). Gate: Conformance Corpus+arithmetic 403/403 · characterization 33/33. Completes the
  arithmetic batch (CA4/CA5/CA6).
- **Spec:** §14.7.7 rule 2b (spec lines 24812-24818): when any operand is a data item described with usage binary-char/-short/-long/-double (or float-short/-long/-extended, a standard floating-point usage, an intrinsic function, or a floating-point literal), "the composite of all OTHER operands shall not contain more than 31 digits." Code: StatementValidation.cs:114-131 (OfExpr/receiver Shape guards); PicInfo.cs:257-269 (BinaryItem digit counts), :369-370 (BinaryCapacity truncation).
- **Verified:** CONFIRMED. §14.7.7 rule 2b requires binary-char/-short/-long/-double operands to be EXCLUDED from the composite (only the other operands are counted, still capped at 31). The Shape guards in CheckComposite screen solely on `{ Category: PicCategory.Numeric, IsFloat: false }` (StatementValidation.cs:118 for operands, :130 for receivers). The four fixed-width binary usages carry Category Numeric and IsFloat false (PicInfo.BinaryItem, :267), so they pass the guard and are wrongly shaped into the composite. PicInfo.BinaryItem (:261-264) assigns them Digits = CHAR 3 / SHORT 5 / LONG 10 / DOUBLE 19 signed·20 unsigned, so a binary-double operand contributes up to 20 integer digits and can push the composite past 31, rejecting a conformant program with COBOLNET0805. I verified the exclusion set: float usages are already excluded by `IsFloat: false`; intrinsic-function operands (BoundIntrinsicCall) fall through OfExpr's switch and are already excluded; COMP-5 is correctly NOT excluded (it is not in the rule-2b list — only the four picture-less binary-N usages are, even though PicInfo groups COMP-5 with them for BinaryCapacity truncation at :369). Only the four binary-N usages are mis-handled. (A related, un-flagged pattern: a floating-point literal is also in rule 2b's list but OfExpr's BoundNumLiteral arm counts its digits as fixed-point — worth a follow-up sweep, not part of this finding.) Applies to COBOL-2002+ where these usages exist; the check is edition-invariant.
- **Fix:** StatementValidation.cs: exclude the four fixed-width binary usages from both Shape sites. Introduce a predicate `static bool InComposite(PicInfo p) => p is { Category: PicCategory.Numeric, IsFloat: false } && p.Usage is not (Usage.BinaryChar or Usage.BinaryShort or Usage.BinaryLong or Usage.BinaryDouble);` and use it in place of the inline patterns: the operand arm at :118 `case BoundNumRef { Place.Item.Pic: { } p } when InComposite(p):` and the receiver loop at :130 `if (r.Place.Item.Pic is { } rp && InComposite(rp))`. This implements rule 2b (the binary-N operands drop out of the superimposition; the composite over the remaining operands is still capped at 31) while keeping COMP-5 and ordinary USAGE BINARY/COMP items counted. No change to the 31-digit cap or the message.
- **Golden (spec-derived):** COBOL (COBOL-2002+ — BINARY-DOUBLE requires 2002):\nIDENTIFICATION DIVISION.\nPROGRAM-ID. CA6.\nDATA DIVISION.\nWORKING-STORAGE SECTION.\n01 BL USAGE BINARY-DOUBLE UNSIGNED VALUE 5.\n01 F  PIC 9(11)V9(13) VALUE 3.\nPROCEDURE DIVISION.\n    ADD BL TO F.\n    DISPLAY F.\n    STOP RUN.\nSPEC-DERIVED (§14.7.7 rule 2b): BL is usage binary-double -> excluded; composite of all OTHER operands = F (11 int + 13 frac = 24 digits) <= 31 -> LEGAL, compiles with no COBOLNET0805. Runtime F = 3 + 5 = 8 -> PIC 9(11)V9(13) = 8.0. EXPECTED OUTPUT (11+13 = 24 implied digits, value 8):\n000000000080000000000000\nCURRENT (bug): CheckComposite shapes BL (20 int, PicInfo.BinaryItem DOUBLE unsigned = 20 digits) and F (11 int, 13 frac) -> max int 20, max frac 13 -> composite 33 > 31 -> emits COBOLNET0805 "the composite of operands spans 33 digits (20 integer + 13 fraction)" and REJECTS the program.

### CA7 · [MAJOR/S] · conditions · ✅ LANDED (DEVLOG 1011)
- **Landed:** in `CobolClass`, changed the `s is null` guard to `string.IsNullOrEmpty(s)` in IsAlphabetic /
  IsAlphabeticUpper / IsAlphabeticLower / IsInClass (matching IsNumeric/IsNumericZoned) so a zero-length operand is
  FALSE for every class test (§8.8.4.4.4 GR1); the RenderClass `Negated ? !test : test` wrapper delivers GR2 under NOT.
  Runtime-only (characterization byte-identical). Golden `2014/ca7_class_zero_length` (DYNAMIC LENGTH → NOTALPHA /
  NOT-ALPHA-TRUE). Gate: Corpus+Class+Condition 478/478 · characterization 33/33 · full Conformance.
- **Spec:** §8.8.4.4.4 GR1 (+GR2, GR3)
- **Verified:** SPEC: §8.8.4.4.4 GR1 (specs/ISO_COBOL.md:9774) — 'If the data item referenced by identifier-1 is a zero-length item, the truth value of the class condition without the word NOT is false.' GR2 (:9776) reverses under NOT; GR3 (:9778) — the per-class membership rules apply ONLY when the item is NOT zero-length. So EVERY class test (ALPHABETIC/-LOWER/-UPPER, alphabet-name, class-name, NUMERIC, BOOLEAN) on a zero-length operand is FALSE (without NOT). CODE: src/Cobol.Net.Runtime/Values/Text/CobolClass.cs — IsAlphabetic (:28-34), IsAlphabeticUpper (:37-43), IsAlphabeticLower (:46-52), IsInClass (:83-89) only null-check then iterate, so an empty string falls through the loop and returns TRUE (IsInClass's own doc-comment at :82 asserts 'an empty value is true vacuously'). IsNumeric (:19-25) and IsNumericZoned (:59-77) correctly guard string.IsNullOrEmpty => false, so NUMERIC is accidentally spec-correct and only the alphabetic/user-class tests diverge. ConditionRenderer.RenderClass (:267-287) emits the runtime call ('A'/'U'/'L' arms :281-283) with no zero-length guard and wraps NOT at :286; BoundUserClassCondition (:70-72) calls IsInClass — none folded, so the wrong value reaches the program. A zero-length operand yields '' via OperandText.AsString (an item is '' iff it is zero-length: a length-N>=1 field always renders N chars), so the empty guard is EXACTLY GR1. Only caller of these greenfield methods is ConditionRenderer (grep: the src/CobolSharp.Runtime/PicRuntime *Class methods are the separate legacy engine), so the fix is collateral-free. Editions: zero-length items are 2002+; reachable via a DYNAMIC-LENGTH item (2014+), an OCCURS-DEPENDING group with count 0, or ref-mod X(1:0) under >>REF-MOD-ZERO-LENGTH (2023).
- **Fix:** src/Cobol.Net.Runtime/Values/Text/CobolClass.cs: add `if (string.IsNullOrEmpty(s)) return false;` as the first line of IsAlphabetic (before :30-31), IsAlphabeticUpper (before :39-40), and IsAlphabeticLower (before :48-49); in IsInClass change :85 `if (s is null) return false;` to `if (string.IsNullOrEmpty(s)) return false;` and correct the doc-comment (:82) from 'an empty value is true vacuously' to 'a zero-length item is false (§8.8.4.4.4 GR1)'. Mechanism: this makes all class predicates uniformly return false for a zero-length operand (matching IsNumeric/IsNumericZoned); RenderClass's existing `c.Negated ? !(test) : (test)` wrapper then delivers GR1 (false, no NOT) and GR2 (true, with NOT) correctly. No renderer change needed.
- **Golden (spec-derived):** IDENTIFICATION DIVISION.
PROGRAM-ID. CLSZERO.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-D PIC X DYNAMIC LENGTH.
PROCEDURE DIVISION.
MAIN-PARA.
    MOVE "" TO WS-D
    IF WS-D IS ALPHABETIC
        DISPLAY "ALPHA"
    ELSE
        DISPLAY "NOTALPHA"
    END-IF
    STOP RUN.
EXPECTED (spec-derived): 'NOTALPHA'. Derivation: after MOVE "" TO WS-D the dynamic-length item WS-D has current length 0, i.e. a zero-length item. §8.8.4.4.4 GR1 makes 'WS-D IS ALPHABETIC' (no NOT) FALSE, so the ELSE branch runs => 'NOTALPHA'. CURRENT CODE emits CobolClass.IsAlphabetic("") which returns true (empty loop) => THEN branch => 'ALPHA'. (Symmetric case: 'IF WS-D IS NOT ALPHABETIC' — spec GR1+GR2 => TRUE; code !true => false.) Editions: 2014 and 2023 (DYNAMIC LENGTH is 2014+).

### CA9 · [MAJOR/M] · exceptions-ec · ✅ LANDED (DEVLOG 1086)
- **Spec:** §13.18.5 GR3/GR4 (spec:17462/17464); §14.9.39 Format 10 GR18/GR19 (EC-DATA-PTR-NULL / EC-SIZE-ADDRESS); Table 13 (all Fatal); §14.6.13.1.3 #5 (spec:24216)
- **Verified:** SPEC: §13.18.5 GR3 — referencing a based item while its address is NULL 'sets EC-DATA-PTR-NULL to exist'; GR4 — a non-NULL invalid address sets EC-BOUND-PTR; §14.9.39 Format 10 GR19 — a non-integer SET pointer UP/DOWN BY sets EC-SIZE-ADDRESS. All Fatal (Table 13). §14.6.13.1.3 #5: with checking enabled and an applicable USE, the declarative executes (RESUME may continue, NOTE 2). CODE: CobolPtr.cs Deref (:26-38), UpBy (:53-58), UpByScaled (:70-72) do `throw new CobolFatalException(...)` UNCONDITIONALLY. The referencing statement is never wrapped (EcBinder.EcWrap QueryFor has no BASED/pointer case; FatalAmbientGates omit these names), so no `catch(CobolFatalException) when(...)` exists; the throw propagates to ProgramTable.RunMain:107 which does only AbnormalTermination (its own comment even names 'a NULL BASED deref' as reaching this surface). Result: under checking ON with an applicable USE AFTER EXCEPTION CONDITION EC-DATA-PTR-NULL declarative, the declarative never runs and RESUME is impossible — the run unit aborts. The checking-OFF loud throw is a defensible §14.6.13.1.3 #8 choice; the checking-ON-with-handler case is the divergence. Distinct from V54–V59.
- **Fix:** Extend the fatal-ambient-gate machinery to the pointer ECs. (1) ExceptionState.cs: add `DataPtrNullChecking`/`BoundPtrChecking`/`SizeAddressChecking` flags + `DataPtrNullError`/`BoundPtrError`/`SizeAddressError(detail)` helpers that `Set(name, fatal:true)` then throw (pattern of RefModError:232). (2) CobolPtr.cs:26-38/53-58/70-72: route each throw through the matching helper so the last-exception status is set before the throw carries the (already correct) EC name; keep an unconditional loud throw when the flag is OFF (the #8 implementor choice). (3) EcEmitter.cs FatalAmbientGates (:109-116): add `("EC-DATA-PTR-NULL","DataPtrNullChecking")`, `("EC-BOUND-PTR","BoundPtrChecking")`, `("EC-SIZE-ADDRESS","SizeAddressChecking")`. (4) EcBinder.cs EcWrap ambient block (:388-409): add conservative whole-statement gates `if (ctx.EcState.Turn.Enabled("EC-DATA-PTR-NULL", null, line)) enabled.Add(...)` for the three names (a BASED-deref-free statement in a checking-on region is harmless — the catch never fires). The statement guard then catches the throw and dispatches to __EcPerform/__EcDispatch, enabling RESUME.
- **Golden (spec-derived):** IDENTIFICATION DIVISION. PROGRAM-ID. CA9. DATA DIVISION. WORKING-STORAGE SECTION. 01 B PIC X(4) BASED. PROCEDURE DIVISION. DECLARATIVES. H SECTION. USE AFTER EXCEPTION CONDITION EC-DATA-PTR-NULL. DISPLAY "HANDLED". RESUME AT KEEP-GOING. END DECLARATIVES. M SECTION. >>TURN EC-DATA-PTR-NULL CHECKING ON  DISPLAY B  . KEEP-GOING. DISPLAY "OK". STOP RUN.  — Edition COBOL-2023. Spec-DERIVED: B's implicit data-address pointer is NULL initially (§13.18.5 GR2); DISPLAY B references it while NULL ⇒ §13.18.5 GR3 sets EC-DATA-PTR-NULL to exist (Fatal). Checking enabled + applicable USE ⇒ §14.6.13.1.3 #5 runs the declarative ('HANDLED'); RESUME AT KEEP-GOING ≡ GO TO (§14.9.33.4 GR3) transfers to the nondeclarative paragraph ⇒ 'OK'. EXPECTED: 'HANDLED' then 'OK', exit 0. ACTUAL (code): CobolPtr.Deref throws unconditionally, no wrapper catches it, RunMain aborts — neither 'HANDLED' nor 'OK' printed, nonzero exit.

### V54 · [MAJOR/S] · intrinsics · ✅ LANDED (DEVLOG 1002)
- **Spec:** §15.59.1 (MAX) / §15.63.1 (MIN) result-type tables: National argument → National function type; §14.9.25.4 Table 16 (National → Alphanumeric = 'No')
- **Verified:** Spec: the MAX/MIN type tables map a National argument to a National function type. Code: IntrinsicBinder.cs:269 does `if (sig.Name is "MAX" or "MIN") category = PicCategory.Alphanumeric;` UNCONDITIONALLY inside the `args.All(IsStringOperand)` block (:259) — and IsStringOperand (:658-669) explicitly admits National operands. So MAX/MIN over all-national arguments produce a result mis-categorized Alphanumeric. Same class of defect as CA25 (result category not following the national argument) but in the MAX/MIN binder block rather than the catalog; same downstream consequences via MoveBinder.cs:162 (Table-16 legality) and comparison collation. The finding's cited fix (resolve the §15.59.1/§15.63.1 table: national→National, all-index→Index, alphabetic/alphanumeric→Alphanumeric) is correct.
- **Fix:** IntrinsicBinder.cs:257-270 — replace the flat `category = PicCategory.Alphanumeric` with a §15.59.1/§15.63.1 table resolution over the bound args: if every arg's OperandCategory (:500-508) is National → `category = PicCategory.National` (and select the national-weighted MaxString/MinString overload from the CA23 fix); if alphabetic/alphanumeric → Alphanumeric; (index args are handled by the numeric MaxScaled path, not IsStringOperand). This dovetails with the CA23 collation fix (same block) — do both together. ORD-MAX/ORD-MIN keep an integer (ordinal) result but dispatch comparison by the same argument category.
- **Golden (spec-derived):** IDENTIFICATION DIVISION. PROGRAM-ID. V54. DATA DIVISION. WORKING-STORAGE SECTION. 01 N1 PIC N(3) VALUE N"ABC". 01 N2 PIC N(3) VALUE N"XYZ". 01 A PIC X(6). PROCEDURE DIVISION. MOVE FUNCTION MAX(N1 N2) TO A. STOP RUN. — Spec-derived (§15.59.1: MAX over national args is category National; §14.9.25.4 Table 16 National→Alphanumeric = 'No'): REJECTED at compile with COBOLNET0819 (national sender → alphanumeric receiver invalid). Current code: MAX(N1 N2) is categorized Alphanumeric (binder :269), the MOVE compiles, and the raw national code units of the selected value are copied into PIC X A. Editions: 2002/2014/2023 (national data is 2002+).

### V55 · [MAJOR/L] · oo · ✅ LANDED (DEVLOG 1089)
- **Spec:** §14.9.23.4 GR7c (line 28207); §14.6.13.1.1 NOTE 3 (line 24165) + §14.6.13.1.3 #4/#5
- **Verified:** SPEC: §14.9.23.4 GR7c — for a universal-object-reference INVOKE of a COBOL method, the §14.8.2/§14.8.3 conformance rules apply at runtime, and on a violation 'the EC-OO-UNIVERSAL exception condition is set to exist IF CHECKING FOR IT IS ENABLED IN BOTH THE ACTIVATED METHOD AND THE ACTIVATING RUNTIME ELEMENT, the method invocation is not successful, and execution continues as specified in General rule 7g' (→ §14.6.13 handling). Two spec facts: (a) EC-OO-UNIVERSAL is set-to-exist ONLY when checking is enabled in BOTH elements (a stricter gate than the general single-side §14.6.13.1.1 rule); (b) when it IS set, GR7g routes to §14.6.13.1.3 where an enabled fatal with an applicable USE/WHEN runs the handler (RESUME possible). CODE: OoEmitter.EmitCobolInvoke generates the per-class __CobolInvoke switch with BARE `throw new CobolFatalException("EC-OO-UNIVERSAL", ...)` at OoEmitter.cs:266 (arity), :274 (per-arg descriptor), :280 (RETURNING present but none declared), :287 (RETURNING absent/nonconforming) — UNCONDITIONAL, with the comment (:246-249) explicitly asserting it follows 'the EC-OO-NULL/METHOD precedent' of raising unconditionally. This diverges two ways: (1) it raises regardless of the 'enabled in both' gate (GR7c) — misreporting EC-OO-UNIVERSAL as raised even when neither/one side enabled, where the spec says it is NOT set to exist (§14.6.13.1.1 NOTE 3: results undefined, implementor may terminate — but must not attribute a not-set EC); (2) it is never routed through dispatch — the universal INVOKE is not wrapped (EcBinder.EcWrap has no BoundInvokeUniversal case) and EC-OO-UNIVERSAL is in no FatalAmbientGate, so the throw reaches ProgramTable.RunMain:107 and terminates (RunMain's own comment names 'an OO __CobolInvoke EC-OO-UNIVERSAL' as such an unhandled surface). Firm confirmation = the checking-enabled-in-both case with an applicable handler: the USE/WHEN is bypassed. The 'raised-when-not-enabled-in-both' half is a weaker, spec-attribution divergence (defensible-as-terminating under #8 but the reported name is wrong).
- **Fix:** Extends the CA11 fix to EC-OO-UNIVERSAL PLUS the 'enabled in both' gate. (1) EcBinder.cs — add EC-OO-UNIVERSAL to the OO-invoke name set and a `case BoundInvokeUniversal` (or extend the BoundInvoke case) so a universal INVOKE under checking binds BoundEcChecked. (2) EcEmitter.cs — include EC-OO-UNIVERSAL in the OO-fatal catch so a matching USE AFTER EXCEPTION CONDITION EC-OO-UNIVERSAL declarative / F3 WHEN runs and RESUME works. (3) OoEmitter.cs:266/274/280/287 — gate the generated `throw` on a runtime checking flag so it is raised-as-EC only when enabled; because the descriptor mismatch is detected inside the CALLEE's __CobolInvoke (which does not know the ACTIVATOR's TURN state), the 'enabled in BOTH' requirement means threading the activating element's checking state to the callee — e.g. set a run-unit/thread ExceptionState.OoUniversalChecking bit in the checked wrapper around EmitUniversalInvoke (activator side) AND emit the callee guard as `if (mismatch) { if (ExceptionState.OoUniversalChecking && <method-side-enabled>) throw CobolFatalException("EC-OO-UNIVERSAL", ...); else throw <unnamed implementor fatal that does NOT report EC-OO-UNIVERSAL>; }`. When not enabled-in-both, an implementor fatal may still stop the nonconforming typed-native crossing (§14.6.13.1.1 NOTE 3 undefined-results latitude) but must not attribute EC-OO-UNIVERSAL. Correct the OoEmitter.cs:246-249 doc-comment (the 'EC-OO-NULL/METHOD precedent' claim is wrong — GR7c has an explicit checking gate GR5/GR7b do not).
- **Golden (spec-derived):** `>>TURN EC-OO-UNIVERSAL CHECKING ON` covering both the class and the main program (satisfies 'enabled in both'). · CLASS-ID C, instance METHOD-ID M with exactly one formal `01 F PIC 9(4).` (arity 1; body DISPLAY 'M-RAN'). · Main: 01 U USAGE OBJECT REFERENCE (universal). INVOKE C 'NEW' RETURNING U. · DECLARATIVES: `USE AFTER EXCEPTION CONDITION EC-OO-UNIVERSAL` → DISPLAY 'HANDLED' then RESUME AT DONE. · MAIN: `INVOKE U 'M'` (NO USING — arity 0 vs the method's 1 formal → GR7c conformance violation). DONE: DISPLAY 'OK'. STOP RUN.  SPEC (§14.9.23.4 GR7c, enabled in both → GR7g → §14.6.13.1.3 #5 + RESUME NOTE 2): __CobolInvoke's arity check finds the mismatch, EC-OO-UNIVERSAL is set to exist, the declarative runs ('HANDLED'), RESUME transfers to DONE ('OK'), exit 0.  ACTUAL: OoEmitter.cs:266 throws unconditionally, uncaught at RunMain → 'abnormal run-unit termination', exit 1; declarative never runs. Second case (attribution half): remove the class-side/main-side TURN so checking is NOT enabled in both — SPEC: EC-OO-UNIVERSAL is NOT set to exist (name not attributable); ACTUAL: still throws a CobolFatalException named 'EC-OO-UNIVERSAL'. Editions: 2002+ (OO / universal object reference).

### V57 · [MAJOR/S] · exceptions-ec · ✅ LANDED (DEVLOG 1083)
- **Spec:** §14.9.28.4 GR14 (spec:29335), GR20/GR21/GR22 (spec:29354/29358/29360); §14.6.13.1.1 (a condition is set to exist only when checking is enabled)
- **Verified:** SPEC: §14.9.28.4 GR14 — 'An implicit PUSH ALL followed by TURN OFF ALL is assumed at the end of imperative-statement-1. Immediately preceding the END PERFORM phrase, there is an implicit POP ALL...'. imp-2/3/4 (WHEN/OTHER/COMMON) and imp-5 (FINALLY) all execute AFTER imp-1 and BEFORE END-PERFORM, hence under TURN OFF ALL — no EC checking is in effect for statements inside a handler/FINALLY body. GR21 confirms results in imp-2..5 are 'as if in a format 2 PERFORM'. CODE: EcBinder.ExceptionPerform.cs binds imp-1 under the WHEN overlay (:56-57), restores `ctx.EcState.Turn = savedTurn` (the pre-PERFORM base) at :58, then binds imp-2/3/4 (:77-87) and imp-5/FINALLY (:88) while Turn == savedTurn. EcWrap consults `ctx.EcState.Turn.Enabled(...)`, so any `>>TURN ec ON` textually preceding the F3 PERFORM leaks into savedTurn and a handler-body statement that could raise `ec` is wrapped in BoundEcChecked — a spurious EC guard the spec forbids. Post-END-PERFORM behavior (GR22) is already correct (overlay popped at :58). The comment at :70-72 rationalizes this as 'base state per GR21', but GR21 governs re-entry, not the checking state — GR14's TURN OFF ALL is the controlling rule. Single-program observable; mis-handles legal source.
- **Fix:** Bind imp-2..5 under an all-OFF TurnState. (1) TurnState.cs (beside WithImplicitEnable:75): add `public TurnState WithAllDisabledFrom(int handlerLine)` that copies events with Line<handlerLine, splices a synthetic floor `new Ev(handlerLine, ExceptionCatalog.EcAll, null, On:false, WithLocation:false)` (EC-ALL matches every name in Fold/NameMatches, so it shadows every prior enable), then appends events with Line>=handlerLine (so a real handler-local >>TURN still wins — GR14's PUSH/POP model). (2) EcBinder.ExceptionPerform.cs: after :75, set `int handlerLine = whenPhrases.Length > 0 ? whenPhrases[0].Start.Line : p.Stop.Line; ctx.EcState.Turn = savedTurn.WithAllDisabledFrom(handlerLine);` for the imp-2..5 binds (loop :77-87 and FINALLY :88); after :88 restore `ctx.EcState.Turn = savedTurn;` (GR22). Minimal-acceptable alternative that matches the finding's wording exactly: bind imp-2..5 against `TurnState.Empty` (exact TURN OFF ALL; ignores the exotic handler-local >>TURN). Update the :70-72 comment to cite GR14. Add an F3-PERFORM conformance test.
- **Golden (spec-derived):** IDENTIFICATION DIVISION. PROGRAM-ID. V57. DATA DIVISION. WORKING-STORAGE SECTION. 01 S2 PIC X(3). 01 R PIC 9(2) VALUE 0. PROCEDURE DIVISION. >>TURN EC-SIZE-OVERFLOW CHECKING ON  PERFORM  STRING "ABCDEF" DELIMITED BY SIZE INTO S2  WHEN EC-OVERFLOW-STRING  COMPUTE R = 40 + 80  DISPLAY "HANDLER-DONE"  END-PERFORM  DISPLAY "AFTER". STOP RUN.  — Edition COBOL-2023. Spec-DERIVED: 'ABCDEF' (6) into PIC X(3) overflows ⇒ GR14 implicitly enables EC-OVERFLOW-STRING over imp-1, the (nonfatal, Table 13) condition is set and matches the WHEN ⇒ imp-2 runs. Per GR14 all checking is OFF during imp-2, so COMPUTE R = 40+80 = 120 into PIC 9(2) truncates to 20 with NO EC set to exist (§14.6.13.1.1) ⇒ 'HANDLER-DONE' prints; GR20 nonfatal path returns to the implicit CONTINUE after imp-1 ⇒ end of PERFORM ⇒ 'AFTER'. EXPECTED: 'HANDLER-DONE' then 'AFTER', exit 0. ACTUAL (code): imp-2 is bound with savedTurn (EC-SIZE-OVERFLOW still ON), so the handler's COMPUTE is EC-wrapped; on overflow it sets EC-SIZE-OVERFLOW, __EcPerform finds the frame is Handling (GR21 transparent) → __EcDispatch → -3 → fatal, unresumed → throw → abnormal termination BEFORE 'HANDLER-DONE'; neither line prints, nonzero exit.

### V58 · [MAJOR/S] · interprogram · ✅ LANDED (DEVLOG 1085)
- **Spec:** §14.9.18.4 GR1b (specs/ISO_COBOL.md:27382) + GR3 (:27411); §14.9.14.4 GR2 (:27114); §14.6.13.1.3 #8 (:24224); Table 13
- **Verified:** Spec: §14.9.18.4 GR1b — a GOBACK/EXIT PROGRAM RAISING condition 'is raised in the activating runtime element IF checking for that exception condition is enabled in the activating runtime element.' If the activator has NOT enabled checking, the condition is not raised there — execution continues (GR3i / normal continuation). §14.6.13.1.3 #8's implementor latitude applies only AFTER a fatal condition 'exists'; GR1b prevents the condition from being raised in an unchecked activator at all, so #8 is a misapplication. Code: ProgramTable.ApplyPropagationDefault (ProgramTable.cs:134-140) does `if (TakePropagated(out pn, out pf) && pf) throw new CobolFatalException(...)`. It is invoked from CallProgram:216 ONLY on the `!siteHandlesPropagation` path — and siteHandlesPropagation is `ecState.Active` (CallEmitter.cs:46), i.e. this path fires exactly when the activator group is entirely EC-FREE (checking definitely not enabled). Verified staging chain: a called SUB with `>>TURN <fatal-ec> CHECKING ON` + `GOBACK RAISING <fatal-ec>` reaches EmitRaisingStage line 389 (r.Enabled true) → SetPropagating(name,true); on return into an EC-free MAIN, ApplyPropagationDefault throws → RunMain (:107) catches CobolFatalException → AbnormalTermination (nonzero exit). Spec-correct result is normal continuation. The nonfatal arm is already correct (no `&& pf` throw; SetPropagating already set the persisting last-exception status). REFINEMENT to the finding text: the 'MAIN GOBACK RAISING' and 'RunMain:98 path' halves are ALREADY neutralized — EmitGoback (CallEmitter.cs:327) and EmitExitProgram (:350) gate the RAISING staging on `__asCalled` (the C3 fix; __asCalled is set true only by Call, never by the main's Activate — ProgramEmitter.cs:361), so a main-program GOBACK/EXIT PROGRAM RAISING never stages and RunMain:98's ApplyPropagationDefault is a dead no-op. The LIVE divergence is solely the CallProgram:216 EC-free-activator fatal throw. Fixing ApplyPropagationDefault covers both call sites.
- **Fix:** ProgramTable.cs:134-140 — make ApplyPropagationDefault DISCARD the staged propagation and continue instead of throwing. Replace the body with `_owner.Exceptions.TakePropagated(out _, out _);` (consume + drop) and rewrite the doc comment to cite §14.9.18.4 GR1b (the activator has not enabled checking, so the RAISING condition is not raised in it — both fatal and nonfatal are discarded; the returning element's last-exception status, set by SetPropagating, still stands per §14.6.13.1.4). This makes the fatal branch behave like the already-correct nonfatal branch. No change needed at CallEmitter.EmitPropagationPickup (the EC-active-activator path is a separate, name-imprecise refinement out of V58 scope). RunMain:98's call may be left (now a harmless no-op) or removed.
- **Golden (spec-derived):** TWO separately-compiled modules, default --std 2023 (EC machinery + RAISING are 2002+). MAIN (EC-free): `IDENTIFICATION DIVISION. PROGRAM-ID. V58MAIN. PROCEDURE DIVISION. CALL "V58SUB". DISPLAY "AFTER-CALL". STOP RUN.`  SUB (separately compiled): `IDENTIFICATION DIVISION. PROGRAM-ID. V58SUB. PROCEDURE DIVISION.` then directive line `>>TURN EC-BOUND-SUBSCRIPT CHECKING ON` then `GOBACK RAISING EC-BOUND-SUBSCRIPT.` (EC-BOUND-SUBSCRIPT is a level-3, non-EC-USER, FATAL name — §14.9.18.3 SR2 requires a PD-header RAISING only for EC-USER names, so this is legal without one; Table 13 = Fatal). Spec-DERIVED expected (§14.9.18.4 GR1b): MAIN has not enabled checking for EC-BOUND-SUBSCRIPT, so the RAISING condition is NOT raised in MAIN; control transfers to the end of the CALL, `AFTER-CALL` is printed, STOP RUN exits 0. Expected stdout = `AFTER-CALL` and exit code 0. Current buggy behavior: ApplyPropagationDefault throws CobolFatalException at the CALL → abnormal run-unit termination, `AFTER-CALL` never printed, nonzero exit.

### CA12 · [MINOR/M] · exceptions-ec · ❌ REFUTED (DEVLOG 1091)

> ❌ **REFUTED 2026-07-28 — a Format-3 USE declarative CANNOT carry the GLOBAL clause, so the outward walk this finding asks for can never select anything.** §14.9.49.2's printed general formats give `[ GLOBAL ]` to Format 1 (`USE [GLOBAL] AFTER STANDARD … PROCEDURE ON …`) and Format 2 (`USE [GLOBAL] BEFORE REPORTING …`) ONLY; Format 3 is `USE AFTER {EXCEPTION CONDITION | EC} …` with no GLOBAL option, confirmed by RENDERING printed page 804 rather than reading the transcription. Across the whole of §14.9.49 the word GLOBAL occurs five times: Formats 1 and 2, Format 1's figure note, and GR3g/GR4b's references to it — the syntax rules never admit it for Format 3. GR4b selects "a qualifying declarative WITH THE GLOBAL ATTRIBUTE", and no Format-3 declarative can have one, so `__EcDispatch`'s `return -3` tail is CORRECT.
>
> ⚠ The apparent asymmetry — `__RunGlobalUse` walks outward for I-O while the EC dispatch returns -3 — is NOT one spec rule with two behaviours, as this entry and my own earlier notes claimed. It is the correct consequence of only Formats 1 and 2 admitting GLOBAL. The finding's golden (`USE GLOBAL AFTER EXCEPTION CONDITION EC-SIZE`) is not legal COBOL and does not compile, which is how the error surfaced.
- **Spec:** §14.9.49.4 GR3g (spec:32668) + GR4b (spec:32676); §14.6.13.1.3 #7 (fatal terminate)
- **Verified:** SPEC: §14.9.49.4 GR3g (Format 3 USE selection) — after the local level-1/2/3 tiers, 'If no qualifying USE statement is found, and a containing source element contains a USE statement with the GLOBAL clause, the search is repeated as specified in General rule 4' → GR4b climbs to 'a qualifying declarative with the GLOBAL attribute in the next inclusive directly containing source element', repeated outward. GR3g explicitly pulls GR4 in for Format 3, so the outward-GLOBAL continuation applies to non-I-O Format-3 declaratives, not just I-O. CODE: EcEmitter.EmitDispatchSelector (__EcDispatch, EcEmitter.cs:261-295) analyzes only THIS element's Format-3 EcEntries (tiers 3c-3g) and ends `return -3;` with NO __outer walk — the outward GLOBAL call (`__outer.__RunGlobalUse`) is emitted only on the I-O paths (EcEmitter.cs:372-373 in __IoCheckEc, DispatchEmitter.cs:240-241 in __IoCheck). Moreover ProgramEmitter.EmitRunGlobalUse (:259-284) matches ONLY Format-1 GLOBAL declaratives by file-name/open-mode — it never inspects EcEntries — so even a container's `USE GLOBAL AFTER EXCEPTION CONDITION EC-SIZE` is unreachable by the existing walk. A contained program raising a non-I-O EC (e.g. EC-SIZE-OVERFLOW) with no local Format-3 declarative gets -3 → fatal termination instead of the container's GLOBAL declarative. Narrow (needs nested programs + non-I-O EC + a container GLOBAL Format-3 declarative), hence minor; still a genuine gap distinct from V54–V59.
- **Fix:** (1) Add a Format-3 EC-aware global walk. ProgramEmitter.cs: add member `public int __RunGlobalUseEc(string __ec, string __f)` that scans this program's `decls[i].Global` entries whose `EcEntries` match __ec (reuse the __EcDispatch tier predicates restricted to Global declaratives), runs the first match via __RunUse and returns its dispatch code, else `return __outer is {} ? __outer.__RunGlobalUseEc(__ec, __f) : -3;` (climb, GR4b). (2) EcEmitter.EmitDispatchSelector (:292): before `return -3;`, when `dispatch.OuterGlobalUse` emit `int __g = __outer.__RunGlobalUseEc(__ec, __f); if (__g != -3) return __g;` so a resumed/handled container declarative propagates (-1/-2/pc). (3) DispatchEmitter emit-gate: emit __RunGlobalUseEc on any program whose containment chain has a GLOBAL Format-3 declarative (extend the existing OuterGlobalUse/ChainHasGlobalUse test that currently keys on file/mode GLOBAL declaratives to also count EcEntries+Global). No binder change — Global + EcEntries are already recorded by ProcedureTableBuilder. Ship a nested-program EC-SIZE GLOBAL conformance test.
- **Golden (spec-derived):** OUTER: IDENTIFICATION DIVISION. PROGRAM-ID. OUTER. PROCEDURE DIVISION. DECLARATIVES. G SECTION. USE GLOBAL AFTER EXCEPTION CONDITION EC-SIZE. DISPLAY "GLOBAL-HANDLED". END DECLARATIVES. MN SECTION. CALL "INNER". DISPLAY "OUTER-DONE". STOP RUN. // nested: IDENTIFICATION DIVISION. PROGRAM-ID. INNER. DATA DIVISION. WORKING-STORAGE SECTION. 01 R PIC 9(2). PROCEDURE DIVISION. >>TURN EC-SIZE CHECKING ON  COMPUTE R = 99 + 50  DISPLAY "INNER-AFTER". GOBACK. END PROGRAM INNER. END PROGRAM OUTER.  — Edition COBOL-2023. Spec-DERIVED: COMPUTE 149 into PIC 9(2) overflows ⇒ an EC-SIZE-* condition set to exist (checking on, level-2 EC-SIZE enables all children). INNER has no local Format-3 declarative; the container OUTER has a matching USE GLOBAL AFTER EXCEPTION CONDITION EC-SIZE ⇒ §14.9.49.4 GR3g→GR4b selects OUTER's GLOBAL declarative ('GLOBAL-HANDLED'); the condition is Fatal and the declarative completes normally without RESUME ⇒ §14.6.13.1.3 #5 abnormal termination. EXPECTED: 'GLOBAL-HANDLED' printed (in OUTER's data context), then abnormal termination; 'INNER-AFTER' and 'OUTER-DONE' NOT printed. ACTUAL (code): __EcDispatch in INNER returns -3 with no outward walk, INNER throws → RunMain aborts — 'GLOBAL-HANDLED' never prints.

### CA18 · [MINOR/L] · files-io · ✅ LANDED (DEVLOG 1017)
- **Spec:** §14.9.35.4 GR17a-d (line-sequential REWRITE); §9.1.13.7 item 4d ('44' for a line-sequential REWRITE after a '06' read)
- **Verified:** SPEC: §14.9.35 GR17 explicitly defines a legal line-sequential REWRITE: (a) if the preceding READ transferred only part of the record ⇒ '44'; (b) if the new record is longer than the record being replaced ⇒ '44'; (c) if shorter, it is space-padded to the length of the record being replaced and written (a SUCCESS, '00'); (d) if it contains characters outside the implementor-defined line character set ⇒ '71'. §9.1.13.7 item 4d ('44' when rewriting a record whose read returned '06') further confirms the standard contemplates a normal line-sequential REWRITE. A well-formed same-or-shorter line-sequential REWRITE therefore yields '00'; nothing in the spec yields '30' (permanent error) for it. CODE: SequentialConnector.Rewrite (SequentialConnector.cs:454-466) — the seekable in-place path is guarded by `!_lineSequential` (:454), so EVERY line-sequential REWRITE falls through to `return Status = FileStatusCode.PermanentError` ('30') at :466, unconditionally failing a legal statement and leaving the file unchanged. VERIFIED the branch guard and the fall-through; the OPEN I-O existing-file path (:238) already opens the stream ReadWrite/seekable for line-sequential too, so the operation is physically reachable. Real bug — '30' is never a correct outcome for a well-formed line-sequential REWRITE. NOTE (implementor latitude): the exact meaning of 'number of bytes in the record being replaced' for a variable-length line, and whether trailing spaces are trimmed, is implementor-documented (Annex E / §9.1.13 item 114/115); the fix must pick one consistent model, but that latitude does not license '30'.
- **Fix:** SequentialConnector.cs — implement GR17 for the line-sequential branch instead of falling through to '30'. Required pieces: (1) In the `_lineSequential` READ branch, track the last-read line's byte anchor and physical length — a manual delimiter-aware read (replacing bare `ReadLine()`) that records `_lastLineStart` (byte offset of the line's first char) and `_lastLineBytes` (chars before the delimiter), plus a `_lastReadPartial` flag set true when the CA15 over-length '06' path fires. (2) In Rewrite (:454), add a line-sequential arm BEFORE the '30' fall-through: if `_lastReadPartial` ⇒ `Status = RecordSizeViolation` ('44', GR17a); compute the replacement bytes `Fit(image, RecordWidth)` trimmed per the connector's write model; if its length > `_lastLineBytes` ⇒ '44' (GR17b); else right-pad with spaces to `_lastLineBytes` (GR17c) and overwrite exactly `_lastLineBytes` bytes at `_lastLineStart` through `_reader.BaseStream` (seek/write/flush/restore, mirroring the record-sequential path at :456-461), `Status = Success` ('00'). Mechanism: because GR17b/c force the written record to equal the replaced physical-line length, the byte span (and its delimiter position) is invariant, so an in-place overwrite is exact. Keep '30' only for the genuinely non-seekable stream case. Effort is L because it requires delimiter-aware offset tracking in the line-sequential READ path (bare `ReadLine()` discards the byte position) and one implementor-defined byte-count/trailing-space decision.
- **Golden (spec-derived):** PROGRAM-ID LSREW. SELECT F ASSIGN "lsrew.txt" ORGANIZATION LINE SEQUENTIAL FILE STATUS FS. FD F. 01 REC PIC X(5). 01 FS PIC XX (WS). Procedure: OPEN I-O F. READ F. MOVE "WORLD" TO REC. REWRITE REC. DISPLAY "FS=" FS. CLOSE F. STOP RUN. INPUT lsrew.txt = one physical line 'HELLO' (5 chars) + newline. SPEC-DERIVED EXPECTED (§14.9.35 GR17: new record 'WORLD' 5 bytes == replaced 'HELLO' 5 bytes ⇒ success): FS='00' and the file's line becomes 'WORLD'. (Editions: version-invariant; line sequential REWRITE contemplated since COBOL-2002.) CODE ACTUAL (buggy): FS='30', file unchanged ('HELLO' retained).

### CA19 · [MINOR/S] · inspect-string · ✅ LANDED (DEVLOG 1016) — (INSPECT / STRING / UNSTRING — CA19, CA20; both are UNSTRING syntax-rule findings)
- **Spec:** ISO §14.9.48.3 SR4 (UNSTRING identifier-4 receiver categories); enforcement duty §4.2.2 (warning mechanism for explicit syntax-rule violations)
- **Verified:** SR4 (§14.9.48.3, verified verbatim in specs/ISO_COBOL.md:32390): 'Identifier-4 shall be described implicitly or explicitly as usage display and category alphabetic, alphanumeric, or numeric; or as usage national and category national or numeric.' So numeric-edited, alphanumeric-edited, and non-DISPLAY/non-NATIONAL numeric (COMP/COMP-3/BINARY/PACKED/COMP-5/INDEX/float) receivers are NOT permitted. CODE: StringUnstringBinder.BindUnstring (StringUnstringBinder.cs:108-135) resolves identifier-4 at :113 and adds it at :134 with NO category/usage screen — only COUNT IN gets an integer check (:129). The emitter StringEmitter.MoveString then AFFIRMATIVELY compiles+executes the illegal shapes: numeric-edited via the edit-mask arm (StringEmitter.cs:204), alphanumeric-edited via :207, and any non-float non-Index numeric — which includes COMP-3/BINARY/packed — via :219. Only shapes NOT handled affirmatively (float numeric, index, boolean, and — separately — the SR4-LEGAL national) fall to the default runtime loud at :226. So the three SR4-illegal shapes the finding names are silently miscompiled instead of flagged, violating the §4.2.2 duty that SR violations be flaggable. Contrast: the STRING side DOES screen its receiver in the binder (StringUnstringBinder.cs:63-67, BoundUnsupported). VERDICT CONFIRMED. Caveat (out of CA19's scope, do NOT regress): a national receiver is SR4-LEGAL but the emitter has no national arm so it already runtime-louds — a separate deferred-national gap; the fix must keep national in the ALLOWED set. Residual: SR4's 'numeric shall not contain P' sub-rule is not detectable in the current PicInfo model (no HasP flag) — a finer refinement, not the confirmed core.
- **Fix:** StringUnstringBinder.cs — in BindUnstring, insert an SR4 receiver screen immediately after identifier-4 is resolved (after the existing :113-114 `if (ctx.Refs.Resolve(drefs[0]) is not { } target) return …`, before :115). Mechanism mirrors the STRING side (BoundUnsupported → runtime-loud NotImplemented.Run, the codebase's established SR-rejection convention; StatementEmitter.cs:175):

    // SR4 — identifier-4 shall be (usage display + category alphabetic/alphanumeric/numeric) or
    // (usage national + category national/numeric). A fixed-length group (SR10) and a reference-modified
    // slice are alphanumeric-image receivers and are exempt; edited / COMP / packed / COMP-5 / index /
    // float receivers are rejected.
    if (target is not RefModPlace && !target.Item.IsGroup && !UnstringReceiverAllowed(target.Item.Pic))
        return new BoundUnsupported(
            $"UNSTRING INTO '{drefs[0].GetText()}' — a usage-display alphabetic/alphanumeric/numeric or " +
            "usage-national national/numeric receiver is required; edited, COMP, packed, COMP-5, index, and " +
            "float receivers are not permitted (ISO §14.9.48.3 SR4)");

and add the helper next to StrUnstrIsInteger (StringUnstringBinder.cs:176):

    private static bool UnstringReceiverAllowed(PicInfo? pic) => pic is
        { Category: PicCategory.Alphanumeric, EditMask: null }                       // display alphabetic/alphanumeric (not edited)
        or { Category: PicCategory.National }                                        // usage national, category national (SR4-legal; emitter defers)
        or { Category: PicCategory.Numeric, IsFloat: false, Usage: Usage.Display or Usage.National };  // display/national numeric only

The existing StringEmitter.cs:226 default arm stays as defense-in-depth. Ship a Conformance test in the same commit (per feedback_goldens_ship_with_the_feature). NOTE for owner: this uses the STRING side's runtime-loud convention; if true compile-time diagnostics for SR violations are wanted per §4.2.2, migrate BOTH the STRING and UNSTRING SR rejections together (a broader decision, out of scope here).
- **Golden (spec-derived):** Two SR4-illegal receivers, spec-DERIVED expectation = the compiler must FLAG/REJECT (§4.2.2 duty; project convention = loud rejection), NOT compile-and-run.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. UNSTR-SR4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC PIC X(3) VALUE "123".
       01 R1  PIC ZZ9.                 *> numeric-edited — SR4-illegal receiver
       01 R3  PIC 999 USAGE COMP-3.    *> non-DISPLAY numeric — SR4-illegal receiver
       PROCEDURE DIVISION.
           UNSTRING SRC DELIMITED BY SIZE INTO R1.
           UNSTRING SRC DELIMITED BY SIZE INTO R3.
           STOP RUN.

Derivation: R1 is category NUMERIC-EDITED and R3 is category numeric with usage COMP-3 (packed, not display, not national). SR4 permits receivers only of (usage display + alphabetic/alphanumeric/numeric) or (usage national + national/numeric); neither shape is in that set ⇒ both statements violate SR4 ⇒ a conforming processor must indicate the violation. EXPECTED (spec-correct): each UNSTRING is rejected with a diagnostic naming §14.9.48.3 SR4 (in the codebase's convention, a BoundUnsupported ⇒ NotImplementedCobolFeatureException naming SR4 at the statement — no silent extraction). CURRENT (buggy): both compile and run — '123' truncated to 2 chars? no: DELIMITED BY SIZE takes the whole field '123' (R1 reception 3 digit positions) → NumFromAlphanumeric('123')=123 → ZZ9 edit → '123' stored in R1; and 123 packed into R3 — a wrong (unflagged) result. A LEGAL control (01 R2 PIC 999 display) correctly receives '123', proving the divergence is receiver-category-specific. Editions: ALL (85/2002/2014/2023) — UNSTRING is a COBOL-85 verb and its receiver-category SR4 is edition-invariant (StringUnstringBinder.cs:11-15 documents the whole surface as '85, no edition gate).

### CA26 · [MINOR/S] · intrinsics · ✅ LANDED (DEVLOG 1018) — resolved toward the ESTABLISHED Unicode design (owner: Unicode was always intended, spec-permitted), NOT the finding's original 256/Latin-1 recommendation. The native alphanumeric repertoire is UTF-16; the residual `& 0xFF` weights aliasing + `Char(n,weights)` domain cap were widened to the full range (§12.3.7.4 GR7 1.3), CHAR/ORD guards already correct.
- **Spec:** §15.15.3 r2 — 'The value of argument-1 shall be greater than zero and less than or equal to the number of positions in the alphanumeric program collating sequence'; §15.16.3 r2 (CHAR-NATIONAL, 65 536)
- **Verified:** The implementation DOCUMENTS its alphanumeric coded character set as 8-bit Latin-1 = 256 positions in multiple places: the char-set model comment CobolIntrinsics.Text.cs:166 ('the alphanumeric coded set is 8-bit Latin-1'), CONVERT ANUM (:195-198, 1 byte/char, '?'-substitutes code points > 0xFF), DISPLAY-OF/NATIONAL-OF's D-N4 Latin-1↔national correspondence, and the 256-entry non-native alphanumeric PCS weights domain (CobolString.Compare(...,ushort[]) masks `& 0xFF`; IntrinsicBinder.cs:277). Yet CobolIntrinsics.Text.cs:21 CHAR's native path guards `c is < 0 or > 0xFFFF` (accepting n up to 65 536 — the NATIONAL bound), so CHAR(257)..CHAR(65536) return U+0100..U+FFFF and raise nothing, and ORD (:84) is likewise unbounded. Under §15.15.3 r2 with the documented 256-position count, CHAR(257) violates the argument rule and must raise EC-ARGUMENT-FUNCTION (checking on) / return the §15.3 implementor default (the code already uses one space). This is a real internal inconsistency — the native CHAR/ORD path contradicts the implementation's own documented 256-position alphanumeric repertoire. CAVEAT (owner ratification): the 'number of positions' is implementor-defined; if the owner instead declares the native alphanumeric sequence to span full UTF-16 (a PIC X is a UTF-16 string in the typed-native model), the fix moves to the docs/CONVERT/non-native-domain side and CHAR's native path is 'correct'. The preponderance of Latin-1/256 documentation makes 256 the self-consistent, conformant choice.
- **Fix:** Presuming the documented 256-position alphanumeric repertoire (recommended): CobolIntrinsics.Text.cs:21 change `if (c is < 0 or > 0xFFFF)` to `if (c is < 0 or > 0xFF)` so CHAR(n) with n > 256 takes the existing ArgumentError + one-space-default path (leave CHAR-NATIONAL at 0xFFFF, :55). Optionally tighten ORD (:84) so a code point > 0xFF in an alphanumeric argument is out-of-repertoire. If the owner ratifies 65 536 instead, no code change to CHAR — instead correct the Text.cs:166 char-set-model documentation and widen the non-native weights domain; that is the less defensible direction given the pervasive Latin-1 model.
- **Golden (spec-derived):** >>TURN EC-ARGUMENT-FUNCTION CHECKING OFF (default). IDENTIFICATION DIVISION. PROGRAM-ID. CA26. DATA DIVISION. WORKING-STORAGE SECTION. 01 X PIC X. PROCEDURE DIVISION. MOVE FUNCTION CHAR(257) TO X. IF X = SPACE DISPLAY "SPACE" ELSE DISPLAY "OTHER". STOP RUN. — Spec-derived (§15.15.3 r2 with a 256-position alphanumeric PCS: 257 > 256 → EC-ARGUMENT-FUNCTION set; checking off → §15.3 implementor default = the code's one space): prints `SPACE`. Current code: c = 256 ≤ 0xFFFF → returns U+0100 (no exception) → prints `OTHER`. With `>>TURN EC-ARGUMENT-FUNCTION CHECKING ON` the spec raises the fatal condition where the code raises nothing. Editions: all (85+).

### CA3 · [MINOR/S] · accept-display-misc · ✅ LANDED (DEVLOG 1015)
- **Spec:** §8.3.3.6.4 GR6 (Format 3, HIGH-VALUE) + GR7 (Format 4, LOW-VALUE) — "At runtime, when referenced outside the SPECIAL-NAMES paragraph, the high-value/low-value format represents the character … that has the highest/lowest ordinal position in the runtime collating sequence … the alphanumeric program collating sequence is used" (spec lines 6289-6295 / 6299-6313); §14.9.11 DISPLAY
- **Verified:** SPEC: whenever HIGH-VALUE/LOW-VALUE is referenced at runtime (which includes DISPLAY, §8.3.3.6.4 NOTE 2), it is the extreme character of the alphanumeric PROGRAM COLLATING SEQUENCE. CODE: DISPLAY renders each operand via OperandText.AsString(o, num) (AcceptDisplayEmitter.cs:23); AsString dispatches a BoundFigurative to AsStringVisitor.Visit(BoundFigurative) (OperandText.cs:136) which hardcodes `FigurativeConstants.Fill(n.Kind, null)` — the null collate arg makes Fill (FigurativeConstants.cs:57-58 / 84-85) return the native pin U+00FF (HIGH) / U+0000 (LOW), IGNORING the declared PCS. The MOVE path threads ctx.Data.Collating (MoveEmitter.cs:299 → Fill honors hc.HighValue), and the relation-condition path threads it too (ConditionRenderer.cs:240), so `MOVE HIGH-VALUE TO X; DISPLAY X` prints the PCS-highest char while `DISPLAY HIGH-VALUE` prints U+00FF — an internal inconsistency and a GR6 divergence. Only the non-native-PCS case diverges: with no PROGRAM COLLATING SEQUENCE, collate is null and the native pin IS the correct native-sequence extreme, so the common case is byte-stable (this is the 'flagged native-pin' posture noted in FigurativeConstants.cs' own doc-comment). CollatingTable carries the computed HighValue/LowValue (CollatingModel.cs:21; DataBinder.Switches.cs:408-416 implements the §12.3.7 GR8/GR9 extremes).
- **Fix:** Intercept BoundFigurative at the AsString ENTRY (mirroring the existing intrinsic interception), so the collating table reaches the render. (1) E:\CobolSharp\src\Cobol.Net.Compiler\CodeGen\Emit\NumericRenderer.cs (near line 18-23): expose the tables the primary-ctor `ctx` already holds — `internal CollatingTable? Collating => ctx.Data.Collating;` and `internal NationalCollatingTable? NationalCollating => ctx.Data.NationalCollating;`. (2) E:\CobolSharp\src\Cobol.Net.Compiler\CodeGen\Emit\OperandText.cs AsString (line 40-43): add a BoundFigurative arm before the visitor fallback — `: op is BoundFigurative fig ? $"new string({FigurativeConstants.Fill(fig.Kind, num.Collating, null, num.NationalCollating)}, 1)"`. Rationale: cat=null because a bare figurative in DISPLAY/STRING/STOP is an alphanumeric value (§8.3.3.6.4 GR1) ⇒ the alphanumeric PCS applies; width 1 per §8.3.3.6.4 GR3b (length-unspecified context = one character). BYTE-STABILITY: when no PCS is declared num.Collating is null and Fill(kind, null) still returns the native pin, so the 32 characterization snapshots are unaffected; only a declared non-native PCS changes the output. The static Visit(BoundFigurative) at line 136 becomes the unreachable native-pin fallback (may be left as-is or removed).
- **Golden (spec-derived):** All editions (PROGRAM COLLATING SEQUENCE / ALPHABET is pre-85). Program:
 IDENTIFICATION DIVISION.
 PROGRAM-ID. FIGDISP.
 ENVIRONMENT DIVISION.
 CONFIGURATION SECTION.
 OBJECT-COMPUTER. XX PROGRAM COLLATING SEQUENCE IS REV.
 SPECIAL-NAMES.
     ALPHABET REV IS X"FF" THRU X"00".
 DATA DIVISION.
 WORKING-STORAGE SECTION.
 01 WS-X PIC X.
 PROCEDURE DIVISION.
     MOVE HIGH-VALUE TO WS-X.
     DISPLAY WS-X.
     DISPLAY HIGH-VALUE.
     STOP RUN.
SPEC-DERIVED EXPECTED (§8.3.3.6.4 GR6): REV reverses the 256 code points, so the highest-ordinal char is X"00"; HIGH-VALUE at runtime = byte 0x00. BOTH DISPLAY lines must emit byte 0x00 (each + newline).
CODE PRODUCES (buggy): line1 (moved) = byte 0x00 (correct, MOVE threads the PCS), line2 (bare figurative) = byte 0xFF (native pin, PCS ignored) — the two paths disagree. LOW-VALUE is symmetric (spec 0xFF, bare-figurative DISPLAY emits 0x00).

### CA30 · [MINOR/S] · oo · ✅ LANDED (DEVLOG 1080)
- **Spec:** §14.9.39.3 SR10d (spec lines 30988-30992; Format 5)
- **Verified:** SPEC: §14.9.39.3 SR10 (SET Format 5) — 'If identifier-4 is specified and the data item referenced by identifier-3 is described with an interface-name that identifies the interface int-1, the data item referenced by identifier-4 shall be one of the following: ... d) the predefined object reference SELF, subject to the following rules: ... 2. if the SET statement is contained in a method within the instance definition of the class, that instance definition shall be described with an IMPLEMENTS clause that references int-1' (and SR10d1 for a factory method). This is a SYNTAX RULE = a compile-time conformance requirement. CODE: OoBinder.OoBindSetObjectRef's SELF-conformance loop (OoBinder.cs:755-760) resolves the target's declared name only via `host.OoClasses?.Find(tcn)` — a CLASS-ONLY lookup. An interface-typed target (01 R USAGE OBJECT REFERENCE I) stores the interface name in Pic.ObjectClassName (DataBinder.cs:1954 confirms ObjectClassName may be a class via Find OR an interface via FindInterface), so `Find(tcn)` returns null for an interface, the `is { } tcls` pattern fails, and NO conformance check runs — SR10d is entirely absent. The data-SENDER path (OoBinder.cs:768-773) by contrast routes through OoConformance.ObjectRefWideningMismatch, which DOES handle an interface-typed receiver (OoConformance.cs:303-329: FindInterface + ImplementsClosure) — proving the SELF path is the asymmetric gap. The emitter (OoEmitter.cs:381-382) then unconditionally renders `R = (I?)(this)`: for a non-sealed containing class this compiles but throws a raw runtime InvalidCastException; for a sealed class Roslyn emits a CS00xx cast error on generated user source — either way a non-COBOL error surface instead of the mandated clean diagnostic (violates the G4 'no Roslyn CS on user source' rule).
- **Fix:** src/Cobol.Net.Compiler/Binding/Procedure/Verbs/OoBinder.cs:755-760 — add an interface branch to the SELF loop. Replace the single `if (... Find(tcn) is { } tcls && !cur.ConformsTo(tcls))` with: for each target `tp` whose `Pic.ObjectClassName is { } tcn`: if `host.OoClasses?.Find(tcn) is { } tcls` keep the existing SR12c2 class check (`!cur.ConformsTo(tcls)` → COBOLNET0867); ELSE IF `host.OoClasses?.FindInterface(tcn) is { } tiface`, enforce SR10d: `if (!host.OoClasses.ImplementsClosure(cur, host.OoInFactory).Contains(tiface)) ctx.Edition.Error("COBOLNET0867", $"SET '{tp.Item.CobolName}' TO SELF: the {(host.OoInFactory ? "factory" : "instance")} definition of class '{cur.Name}' does not IMPLEMENT interface '{tiface.Name}' (ISO §14.9.39.3 SR10d)");`. Mechanism: `host.OoInFactory` (already used at OoBinder.cs:171/223) selects SR10d1 (factory definition) vs SR10d2 (instance definition); OoClassTable.ImplementsClosure(cls, factory) is the §11.8.4 GR2 transitive IMPLEMENTS closure (same helper ObjectRefWideningMismatch uses at OoConformance.cs:306); FindInterface/Find already exist. A universal target (ObjectClassName null) stays unconstrained (correct — SR8).
- **Golden (spec-derived):** INTERFACE-ID I. PROCEDURE DIVISION. METHOD-ID MP. END METHOD MP. END INTERFACE I.  //  CLASS-ID C.  (NO `IMPLEMENTS I`)  OBJECT. ... METHOD-ID DOIT. DATA DIVISION. WORKING-STORAGE SECTION. 01 R USAGE OBJECT REFERENCE I. PROCEDURE DIVISION. SET R TO SELF. END METHOD DOIT. END OBJECT. END CLASS C.  ||  SPEC-DERIVED EXPECTED (§14.9.39.3 SR10d2): the SET is in an instance method of C, R is interface-typed I, and C's instance definition has no IMPLEMENTS I → SYNTAX-RULE violation → a clean compile-time COBOL diagnostic (COBOLNET0867) rejecting the program.  ACTUAL (bug): Find('I') is null so the check is skipped, the SET binds clean; the emitter renders `R = (I?)(this)` → non-sealed C: runtime InvalidCastException; sealed C: a Roslyn CS cast error on generated source — neither is the mandated COBOL diagnostic. Positive control: add `IMPLEMENTS I` (with a conforming MP) to C's OBJECT → SET R TO SELF must compile and run. Editions: 2002+ (OO interfaces).

### CA37 · [MINOR/M] · tables-refmod · ✅ LANDED (DEVLOG 1090)
- **Spec:** §14.9.39 GR31 (spec line 31364); Table 13 (EC-FLOW-SEARCH = Fatal, spec line 24359); §14.6.13.1.3 #4/#5 (+ #8 for checking-off); §14.6.13.1.1 NOTE 3
- **Verified:** SPEC: §14.9.39 GR31 (spec line 31364, verbatim): 'This statement shall not be executed during the execution of a SEARCH statement referring to the same table. If this rule is violated, the EC-FLOW-SEARCH exception condition is set to exist and the SET statement is not executed.' EC-FLOW-SEARCH is Fatal (Table 13). Fatal handling §14.6.13.1.3: #4 — if the SET is inside imperative-statement-1 of a PERFORM with a matching WHEN, run that WHEN then abnormally terminate (RESUME may continue, NOTE 1); #5 — if checking enabled and an applicable USE declarative exists, run it then abnormally terminate (RESUME may continue, NOTE 2); #7 — checking enabled, no handler → abnormal termination; #8 — checking NOT enabled → implementor-defined whether execution continues. CODE: CobolDynTable.SetCapacity (CobolDynTable.cs:110-118) does `if (_searching > 0) throw new CobolFatalException("EC-FLOW-SEARCH", ...)` UNCONDITIONALLY — no consultation of any checking flag. EcBinder.QueryFor (EcBinder.cs:319-410) has NO case for BoundSetCapacity and no EC-FLOW-SEARCH ambient gate, and neither EcEmitter.FatalAmbientGates (:109-116) nor NonfatalAmbientGates (:74-78) lists EC-FLOW-SEARCH; so the SET is never wrapped in BoundEcChecked. SetEmitter.EmitSetCapacity (SetEmitter.cs:66-77) emits a bare `.SetCapacity(tmp)` with no try/catch. The SEARCH's EnterSearch/ExitSearch bracket (ControlFlowEmitter.cs:355-359) is try/FINALLY (no catch). ProgramTable.RunMain (:107) catches CobolFatalException only to AbnormalTermination — no USE dispatch. EC-FLOW-SEARCH is a registered turnable Fatal L3 name (ExceptionCatalog.cs:105). NET: under `>>TURN EC-FLOW-SEARCH CHECKING ON`, the throw propagates uncaught → abnormal termination BYPASSING #4/#5 handler dispatch; RESUME is impossible; a matching USE/PERFORM-WHEN never runs. This is the CONFIRMED divergence (checking-ON). The checking-OFF case (default) is defensible-as-conformant under #8 (implementor may terminate), though it also breaks the codebase's own auto-raised-fatal doctrine of returning leniently when off (cf. ExceptionState.RefModError:232-239, PerformVaryingIndexError:251-258, FloatNotFiniteError:273-280).
- **Fix:** Shared architectural fix with CA38 (mirror the FatalAmbientGates pattern). RUNTIME src/Cobol.Net.Runtime/Exceptions/ExceptionState.cs: add `public bool FlowSearchChecking { get; set; }` and `public void FlowSearchError(string detail){ if(FlowSearchChecking){ Set("EC-FLOW-SEARCH", fatal:true); throw new CobolFatalException("EC-FLOW-SEARCH", detail); } }` on ExceptionEngine, plus the static forwarders (mirror BoundRefModChecking/RefModError at :223-239 and :485-493). RUNTIME src/Cobol.Net.Runtime/Values/Tables/CobolDynTable.cs:112-114: replace `if (_searching > 0) throw new CobolFatalException("EC-FLOW-SEARCH", ...)` with `if (_searching > 0) { ExceptionState.FlowSearchError("SET of a dynamic-capacity table's capacity during a SEARCH of that same table (ISO §14.9.39 GR31)"); return; }` — the `return` (when checking OFF, FlowSearchError is a no-op) realizes GR31's 'the SET statement is not executed' as a clean skip-and-continue instead of a fatal abort, matching the sibling auto-raise doctrine. COMPILER src/Cobol.Net.Compiler/CodeGen/EcEmitter.cs:109-116: add `("EC-FLOW-SEARCH", "FlowSearchChecking")` to FatalAmbientGates. COMPILER src/Cobol.Net.Compiler/Binding/Procedure/Verbs/EcBinder.cs QueryFor (~:368): add a PRECISE case `case BoundSetCapacity: Query(["EC-FLOW-SEARCH"]); break;` (SET Format 14 is the only EC-FLOW-SEARCH raise site per GR31 — a precise case like the EC-RANGE-PERFORM-VARYING one, not a blanket ambient gate). Then EmitArgOrPlain (:118-146) wraps the SET in try/catch(CobolFatalException when EcName=="EC-FLOW-SEARCH"), sets the last-exception status, calls EcDispatchExpr for USE/F3 dispatch, honors RESUME (>=0 → __pc; -2 → continue), else rethrows to terminate (§14.6.13.1.3 #5/#7).
- **Golden (spec-derived):** Editions: 2023 (OCCURS DYNAMIC + SET Format 14 capacity are 2023). Program (OCCURS DYNAMIC per §13.18.38 Format 4):
>>TURN EC-FLOW-SEARCH CHECKING ON
 IDENTIFICATION DIVISION.
 PROGRAM-ID. CA37.
 DATA DIVISION.
 WORKING-STORAGE SECTION.
 01 T.
    05 E PIC 9 OCCURS DYNAMIC CAPACITY IN CAP INDEXED BY IX.
 PROCEDURE DIVISION.
 DECLARATIVES.
 H-SEC SECTION.
     USE AFTER EXCEPTION CONDITION EC-FLOW-SEARCH.
 H-PARA.
     DISPLAY "DECL-RAN".
     RESUME AT CONT-PARA.
 END DECLARATIVES.
 M-SEC SECTION.
 M-PARA.
     SET E CAPACITY TO 2
     MOVE 5 TO E(1)
     SET IX TO 1
     SEARCH E WHEN E(IX) = 5
         SET E CAPACITY UP BY 1
     END-SEARCH.
 CONT-PARA.
     DISPLAY "AFTER"
     DISPLAY CAP
     STOP RUN.
SPEC-DERIVED EXPECTED OUTPUT:
DECL-RAN
AFTER
0000000002
Derivation: SEARCH matches at IX=1 (E(1)=5); the WHEN imperative `SET E CAPACITY UP BY 1` executes during the SEARCH of the same table → §14.9.39 GR31 violated → EC-FLOW-SEARCH set to exist (checking enabled) and 'the SET statement is not executed' (so CAP stays 2). EC-FLOW-SEARCH is Fatal → §14.6.13.1.3 #5: the applicable USE AFTER EXCEPTION CONDITION EC-FLOW-SEARCH declarative executes → 'DECL-RAN'; the declarative's RESUME (NOTE 2) transfers to CONT-PARA → 'AFTER' and CAP=2 (the SET was not executed). CURRENT CODE: SetCapacity throws EC-FLOW-SEARCH uncaught → ProgramTable.RunMain AbnormalTermination, exit nonzero; NOTHING is printed, the declarative never runs, RESUME impossible. (CAP's DISPLAY image is the 10-digit implementor capacity register per DataBinder.Odo.cs:272; the load-bearing facts are DECL-RAN + AFTER print and CAP is unchanged.)

### CA38 · [MINOR/M] · tables-refmod · ✅ LANDED (DEVLOG 1090)
- **Spec:** §14.9.39 GR30 (spec line 31350); §8.5.1.9.6 GR2 (spec line 8182); Table 13 (EC-BOUND-TABLE-LIMIT = Fatal, spec line 24321); §14.6.13.1.3 #4/#5 (+ #8 for checking-off); §14.6.13.1.1 NOTE 3
- **Verified:** SPEC: §14.9.39 GR30 (spec line 31350, verbatim): 'If the new capacity of the table exceeds the implementor's maximum capacity for this dynamic-capacity table, the EC-BOUND-TABLE-LIMIT exception condition is set to exist and the capacity of the table is unchanged'. §8.5.1.9.6 GR2 confirms it is Fatal and resource-bounded. Table 13 (line 24321): Fatal. Fatal handling per §14.6.13.1.3 #4/#5 (matching PERFORM WHEN / USE declarative runs, RESUME may continue) / #7 (checking on, no handler → abnormal termination) / #8 (checking off → implementor-defined). CODE: CobolDynTable.GrowTo (CobolDynTable.cs:91-105) does `if (newCount > MaxOccurrences) throw new CobolFatalException("EC-BOUND-TABLE-LIMIT", ...)` (:94-96) UNCONDITIONALLY — no checking-flag gate — which is the exact anti-pattern its own file-sibling avoids: BoundOverflowError (ExceptionState.cs:214-217, invoked from CobolDynTable.RefReceiving:78) gates on BoundOverflowChecking. EC-BOUND-TABLE-LIMIT is in neither FatalAmbientGates nor NonfatalAmbientGates (EcEmitter.cs:74-116), and EcBinder.QueryFor has no case enabling it; so no statement (neither SET Format 14 via SetCapacity→GrowTo, nor implicit growth via a receiving subscript RefReceiving:80→GrowTo) is wrapped for USE/F3 dispatch. SetEmitter.EmitSetCapacity emits a bare call; ProgramTable.RunMain (:107) only AbnormalTermination. EC-BOUND-TABLE-LIMIT is a registered turnable Fatal L3 name (ExceptionCatalog.cs:83). NET: under `>>TURN EC-BOUND-TABLE-LIMIT CHECKING ON`, the throw propagates uncaught, bypassing #4/#5 handler dispatch; RESUME impossible. CONFIRMED (checking-ON). The checking-OFF/default case (terminate) is defensible under #8 but diverges from GR30's stated 'the capacity of the table is unchanged' lenient outcome and from the codebase's own gated auto-raise policy. Low severity: growth past the ~1.07e9 (0x3FFF_FFFF) implementor max is a pathological/resource-bound case.
- **Fix:** Shared architectural fix with CA37. RUNTIME src/Cobol.Net.Runtime/Exceptions/ExceptionState.cs: add `public bool BoundTableLimitChecking { get; set; }` and `public void BoundTableLimitError(string detail){ if(BoundTableLimitChecking){ Set("EC-BOUND-TABLE-LIMIT", fatal:true); throw new CobolFatalException("EC-BOUND-TABLE-LIMIT", detail); } }` + static forwarders (mirror the BoundRefMod pattern). RUNTIME src/Cobol.Net.Runtime/Values/Tables/CobolDynTable.cs:94-96: replace the unconditional throw with `if (newCount > MaxOccurrences) { ExceptionState.BoundTableLimitError($"...exceeds the implementor maximum ({MaxOccurrences}) - ISO §8.5.1.9.6"); return; }` — the `return` (no-op when checking OFF) realizes GR30's 'the capacity of the table is unchanged'. Guard the implicit-growth caller: in RefReceiving (:80-82), after `GrowTo((int)occ)` add `if (occ > _count) { _scratch = _seedAt((int)occ); return ref _scratch; }` so a declined (checking-off) grow past max returns a benign scratch slot instead of indexing `_store[occ-1]` out of range. COMPILER src/Cobol.Net.Compiler/CodeGen/EcEmitter.cs:109-116: add `("EC-BOUND-TABLE-LIMIT", "BoundTableLimitChecking")` to FatalAmbientGates. COMPILER src/Cobol.Net.Compiler/Binding/Procedure/Verbs/EcBinder.cs: enable it as a BLANKET ambient gate (not a precise case) mirroring EC-BOUND-REF-MOD at :395-396, because both SET Format 14 AND implicit growth via any receiving subscript can raise it: `if (ctx.EcState.Turn.Enabled("EC-BOUND-TABLE-LIMIT", null, line)) enabled.Add(("EC-BOUND-TABLE-LIMIT", null));`. EmitArgOrPlain then wraps the statement in try/catch, dispatches via EcDispatchExpr, honors RESUME else rethrows (§14.6.13.1.3 #5/#7).
- **Golden (spec-derived):** Editions: 2023 (OCCURS DYNAMIC + SET Format 14). Program:
>>TURN EC-BOUND-TABLE-LIMIT CHECKING ON
 IDENTIFICATION DIVISION.
 PROGRAM-ID. CA38.
 DATA DIVISION.
 WORKING-STORAGE SECTION.
 01 T.
    05 E PIC 9 OCCURS DYNAMIC CAPACITY IN CAP.
 PROCEDURE DIVISION.
 DECLARATIVES.
 H-SEC SECTION.
     USE AFTER EXCEPTION CONDITION EC-BOUND-TABLE-LIMIT.
 H-PARA.
     DISPLAY "DECL-RAN".
     RESUME AT CONT-PARA.
 END DECLARATIVES.
 M-SEC SECTION.
 M-PARA.
     SET E CAPACITY TO 2000000000.
 CONT-PARA.
     DISPLAY "AFTER"
     DISPLAY CAP
     STOP RUN.
SPEC-DERIVED EXPECTED OUTPUT:
DECL-RAN
AFTER
0000000000
Derivation: 2,000,000,000 > the implementor maximum (0x3FFF_FFFF = 1,073,741,823, CobolDynTable.MaxOccurrences — a valid §8.5.1.9.6 GR2 resource-bounded maximum) → §14.9.39 GR30: EC-BOUND-TABLE-LIMIT set to exist (checking enabled) and 'the capacity of the table is unchanged' (CAP stays 0). EC-BOUND-TABLE-LIMIT is Fatal → §14.6.13.1.3 #5: the applicable USE AFTER EXCEPTION CONDITION EC-BOUND-TABLE-LIMIT declarative runs → 'DECL-RAN'; its RESUME (NOTE 2) → CONT-PARA → 'AFTER' and CAP=0 (unchanged). CURRENT CODE: GrowTo throws EC-BOUND-TABLE-LIMIT uncaught → RunMain AbnormalTermination, exit nonzero; nothing printed, declarative never runs, RESUME impossible.

### CA8 · [MINOR/M] · conditions · ✅ LANDED (DEVLOG 1014)
- **Spec:** §8.8.4.7.3 SR2 + §8.8.4.7.4 GR2 (Format 2)
- **Verified:** SPEC: §8.8.4.7.3 (specs/ISO_COBOL.md:9924-9934) partitions the sign condition — Format 1 SR1: arithmetic-expression-1 is 'any single numeric data item described with a usage OTHER THAN a standard floating-point usage, or any form of arithmetic expression' (NOTE: a standard-float item ENCLOSED IN PARENTHESES is Format 1); Format 2 SR2: 'Data-name-1 shall be the name of a single data item ... described with a standard floating-point usage, and that name shall not be enclosed in parentheses.' So a BARE (unparenthesized) float data-name is Format 2. §8.8.4.7.4 GR2 (:9962-9970): POSITIVE is true iff the SIGN of the content is positive per the IEEE-754 basic float interchange format 'regardless of whether the content ... would evaluate to true in a NUMERIC class test or a ZERO sign test' (GR2a); NEGATIVE mirrors it (GR2b); ZERO true iff a valid representation of zero regardless of sign (GR2c). The NOTE confirms +0.0 is BOTH POSITIVE and ZERO, -0.0 is BOTH NEGATIVE and ZERO. CODE: ConditionRenderer.RenderSign (:252-260) renders EVERY sign condition — including a bare float name — with the Format-1 algebraic test `v.Expr > 0` / `< 0` / `== 0` on the IEEE double (:258); it never tests the IEEE sign bit. ConditionBinder.cs:427 builds one BoundSignCondition(Expr,kind,not) with no Format flag (BoundTree.cs:358), so `(FL) IS POSITIVE` (Format 1) and `FL IS POSITIVE` (Format 2) render identically. Divergences: +0.0 IS POSITIVE (spec true / code `0.0>0` false), -0.0 IS NEGATIVE (spec true / code `-0.0<0` false), and NaN/Inf sign tests. ZERO is unaffected (`==0` already covers both signed zeros). Reachable: grammar comparisonExpression alt 2 (CobolExpressions.g4:151) parses a bare float operand; the binder does not reject it. Editions: standard floating-point usages (FLOAT-SHORT/-LONG/-EXTENDED) exist 2014+, so 2014 and 2023.
- **Fix:** Three files. (1) src/Cobol.Net.Compiler/Binding/Bound/BoundTree.cs:358 — add a flag: `BoundSignCondition(BoundExpr Expr, char Kind, bool Negated, bool Format2Float = false)`. (2) src/Cobol.Net.Compiler/Binding/Procedure/Verbs/ConditionBinder.cs:423-428 — before constructing, detect Format 2: the operand parse subtree operands[0] must reduce to a single primaryExpression that is a `dataReference` (NOT the `LPAREN arithmeticExpression RPAREN` alt, and with no addOp/mulOp/POWER/unary-sign nodes — subscript/ref-mod parens INSIDE dataReference are fine per SR2) AND the bound field's Pic usage is a standard floating-point usage (PicInfo.IsFloat). Set Format2Float accordingly (a parenthesized float, a non-float item, or any compound expression stays Format2Float=false => Format 1). (3) src/Cobol.Net.Compiler/CodeGen/Emit/ConditionRenderer.cs RenderSign (:252-260) — when s.Format2Float, emit the IEEE sign-bit test on the double form NumericRenderer.Real(v): 'P' => `!double.IsNegative({Real(v)})`, 'N' => `double.IsNegative({Real(v)})`, 'Z' => `{Real(v)} == 0.0`; keep floatSendingExempt:true (:257) so a NaN/Inf sign test does not raise EC-DATA-NOT-FINITE (§14.6.13.2 dash-2). Non-Format2 keeps the existing algebraic test. Mechanism: double.IsNegative reads the IEEE sign bit (true for -0.0 and negative-signed NaN, false for +0.0), exactly GR2a/GR2b; widening single->double preserves the sign of zero and NaN so a FLOAT-SHORT operand is covered.
- **Golden (spec-derived):** IDENTIFICATION DIVISION.
PROGRAM-ID. SIGNF2.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 FL USAGE FLOAT-LONG.
PROCEDURE DIVISION.
MAIN-PARA.
    MOVE 0 TO FL
    IF FL IS POSITIVE
        DISPLAY "POS"
    ELSE
        DISPLAY "NOTPOS"
    END-IF
    STOP RUN.
EXPECTED (spec-derived): 'POS'. Derivation: MOVE 0 TO FL stores the value zero; the IEEE-754 binary64 standard representation of +0.0 has sign bit 0. 'FL IS POSITIVE' is a bare unparenthesized standard-float name => Format 2 (§8.8.4.7.3 SR2). §8.8.4.7.4 GR2a: POSITIVE is true iff the sign of the content is positive, regardless of the ZERO/NUMERIC result => TRUE => 'POS'. CURRENT CODE emits `(double)FL > 0.0` = `0.0 > 0.0` = false => 'NOTPOS'. (Companion Format-2 case: a FLOAT-LONG holding -0.0 in 'IF X IS NEGATIVE' — spec GR2b TRUE via sign bit, code `-0.0 < 0` false.) Editions: 2014 and 2023.

### V56 · [MINOR/S] · conditions · ✅ LANDED (DEVLOG 1014)
- **Spec:** §8.8.4.2.4 (comparison of numeric operands) + §8.8.1.5.1/.2 (SDIDI)
- **Verified:** SPEC: §8.8.4.2.4 (specs/ISO_COBOL.md:9508-9512): 'When native arithmetic is in effect, comparison proceeds by the rules of native arithmetic'; but 'When standard-decimal arithmetic is in effect ... The comparison is performed as if each operand not already in the format of a standard-decimal intermediate data item had been converted to that form, and the comparison made between the two corresponding standard-decimal intermediate data items.' So under STANDARD-DECIMAL a float operand must be lifted to SDIDI (decimal128, §8.8.1.5.1 implementor-defined float->SDIDI) and compared decimally — NOT as native IEEE double. CODE: ConditionRenderer.RenderRelational — line 151 `if (l.Real || rr.Real) return {Real(l)} {op} {Real(rr)}` fires UNCONDITIONALLY (before the standard-decimal `if (l.Dec || rr.Dec)` branch at :154 and regardless of arithmetic mode). A bare fixed operand read via AsNum is a plain scaled-integer NumX (neither .Real nor .Dec — .Dec is set only by CombineStandardDecimal, NumericRenderer.cs:213-216), so a float-vs-fixed relation takes the native branch. NumericRenderer.Real(fixed) (:339) is `(double)(scaledInt) / 10^scale`, which ROUNDS the fixed operand to double, discarding precision beyond ~16 digits — whereas SDIDI keeps a <=31-digit fixed operand EXACT (§8.8.1.5.2). num.StandardDecimal (NumericRenderer.cs:230) is true for ArithmeticMode.StandardDecimal and Standard, and is set from the OPTIONS ARITHMETIC clause (OptionsBinder.cs:63-67). Divergence is observable when a fixed operand carries precision beyond binary64. The figurative-vs-ZERO float path (:191) has the same shape but NO observable divergence (comparing to zero preserves sign/zero-ness under the conversion), so that leg is a consistency-only fix. Editions: `ARITHMETIC IS STANDARD` is 2002+, STANDARD-DECIMAL 2014+; default --std 2023 accepts both.
- **Fix:** src/Cobol.Net.Compiler/CodeGen/Emit/ConditionRenderer.cs:148-155 (RenderRelational tail). Gate the native branch on native arithmetic and route the float case through SDIDI under standard-decimal. Replace :151 `if (l.Real || rr.Real)` with `if ((l.Real || rr.Real) && !num.StandardDecimal)` (keep its native-double body), and change :154 to `if (l.Dec || rr.Dec || l.Real || rr.Real)` so that under standard-decimal a Real operand (which skipped the native branch) is compared via `CobolDec.Compare({num.DecOperand(l)}, {num.DecOperand(rr)}) {r.Op} 0` — DecOperand (NumericRenderer.cs:238-241) already maps a Real operand to CobolDec.FromDouble and a plain fixed operand to CobolDec.From(expr,scale) exactly. Optionally apply the same !num.StandardDecimal gate at the figurative path :191 for consistency (no behavior change). Mechanism: under native mode behavior is unchanged; under standard-decimal a float-vs-fixed relation now compares the SDIDI intermediates per §8.8.4.2.4, preserving the fixed operand's full decimal precision. Two-fixed-operand relations keep the existing scale-aligned integer compare (:156-157), which is algebraically identical to SDIDI comparison for exact decimals.
- **Golden (spec-derived):** IDENTIFICATION DIVISION.
PROGRAM-ID. RELSDEC.
OPTIONS.
    ARITHMETIC IS STANDARD-DECIMAL.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 F USAGE COMP-2.
01 D PIC 9V9(17) VALUE 1.00000000000000001.
PROCEDURE DIVISION.
MAIN-PARA.
    MOVE 1 TO F
    IF F = D
        DISPLAY "EQ"
    ELSE
        DISPLAY "NE"
    END-IF
    STOP RUN.
EXPECTED (spec-derived): 'NE'. Derivation: standard-decimal is in effect, so §8.8.4.2.4 requires both operands compared as SDIDI (decimal128). F=1.0 (exact double) lifts to SDIDI 1 (§8.8.1.5.1). D = 1.00000000000000001 is an exact 18-significant-digit decimal, representable exactly in the 34-digit SDIDI significand (§8.8.1.5.2). 1 != 1.00000000000000001 => 'NE'. CURRENT CODE takes the native branch (ConditionRenderer.cs:151): `(double)F == (double)(100000000000000001L)/1e17`; (double)100000000000000001L rounds to 1e17 (ULP at 1e17 is 16), so the RHS is 1.0 and the test is `1.0 == 1.0` = true => 'EQ'. Divergence: spec 'NE' vs code 'EQ'. The exact digit at which the collapse occurs is implementor-defined (§8.8.1.5.1 float->SDIDI), but the qualitative divergence — native rounds the fixed operand to double and loses precision that SDIDI keeps — is spec-firm. Editions: 2014 and 2023 (STANDARD-DECIMAL); the analogous native-branch bug also hits `ARITHMETIC IS STANDARD` (2002+).

### CA17 · [NIT/S] · files-io · ✅ LANDED (DEVLOG 1013)
- **Spec:** §14.9.35.4 GR22 (indexed sequential REWRITE '21'); §12.4.5.12.4 GR1 + §14.9.51 GR35/GR42 (record-key equality is collating-sequence-based per relation-condition rules); §9.1.13.5 item 1 ('21')
- **Verified:** SPEC: §14.9.35 GR22 — for a sequential-access indexed REWRITE 'the value of the prime record key of the record to be replaced shall be EQUAL TO the value of the prime record key of the last record read using this file connector. If it is not... '21''. What 'equal' means for an indexed record key is fixed by the pervasive rule that record-key equality is collating-sequence-based: §12.4.5.12.4 GR1 ('The equality or inequality is based on the collating sequence used for the file according to the rules for a relation condition'), reinforced by §14.9.51 GR35 and GR42 ('The comparison for equality for record keys is based on the collating sequence for the file according to the rules for a relation condition'). Under a §12.4.5.7 COLLATING SEQUENCE that weighs two distinct characters equally (e.g. ALPHABET ... "A" ALSO "a"), two byte-different prime-key values are EQUAL, so no change occurred and REWRITE must succeed. CODE: IndexedConnector.Rewrite (IndexedConnector.cs:373) — `if (prime != _lastReadPrime) return Status = SequenceError;` uses C# ORDINAL string inequality, whereas every other prime-key comparison in the connector routes through KeyEq/KeyCompare (:336, :338, :375, :413) which honor `_primeWeights`. VERIFIED `_primeWeights` is wired end-to-end: DataBinder.ResolveFileCollating resolves per-key weights from a §12.4.5.7 clause and KeyedIoEmitter.cs:62 passes `WeightsLit(file.PrimeKeyWeights)` into RegisterIndexed ⇒ IndexedConnector ctor. So the divergence is live, not latent. Genuine bug and internal inconsistency, but nit-severity: it requires a prime-key COLLATING SEQUENCE with equal-weighted distinct characters AND a program that REWRITEs with a collationally-equal-but-byte-different key.
- **Fix:** IndexedConnector.cs:373 — replace `if (prime != _lastReadPrime) return Status = FileStatusCode.SequenceError;` with `if (_lastReadPrime is not { } lastPrime || !KeyEq(prime, lastPrime, -1)) return Status = FileStatusCode.SequenceError;   // '21' §14.9.35 GR22 (equality per §12.4.5.12.4 GR1)`. Mechanism: KeyEq(-1) delegates to KeyCompare with `_primeWeights`, so the '21' change-detection uses the file's prime-key collating sequence exactly like WRITE (GR42) and the uniqueness checks (GR35), eliminating the lone ordinal outlier. The `is not { } lastPrime` guard preserves the current behavior when `_lastReadPrime` is null (still '21'); reachable only when `wasRead` (checked at :372), so lastPrime is normally non-null.
- **Golden (spec-derived):** SPECIAL-NAMES: ALPHABET ALPHA IS "A" ALSO "a", "B" ALSO "b". SELECT F ASSIGN "ixcoll.dat" ORGANIZATION INDEXED ACCESS SEQUENTIAL RECORD KEY K COLLATING SEQUENCE OF K IS ALPHA FILE STATUS FS. FD F. 01 REC. 05 K PIC X(4). 05 D PIC X(6). Setup (ACCESS RANDOM or a prior OUTPUT run) writes one record with K='A123'. Then: OPEN I-O F. READ F NEXT. MOVE "a123" TO K. MOVE "UPDATED" TO D. REWRITE REC. DISPLAY "FS=" FS. CLOSE F. STOP RUN. SPEC-DERIVED EXPECTED (§14.9.35 GR22 + §12.4.5.12.4 GR1: 'A123' and 'a123' are EQUAL under ALPHA ⇒ prime key unchanged ⇒ REWRITE succeeds): FS='00', record replaced. (Editions: indexed COLLATING SEQUENCE OF key is a 2002+ feature; behavior version-invariant thereafter.) CODE ACTUAL (buggy): 'a123' != 'A123' ordinally ⇒ FS='21', REWRITE fails.

### CA20 · [NIT/S] · inspect-string · ✅ LANDED (DEVLOG 1016) — (INSPECT / STRING / UNSTRING — CA19, CA20; both are UNSTRING syntax-rule findings)
- **Spec:** ISO §14.9.48.3 SR2 (UNSTRING identifier-1 sender category); enforcement duty §4.2.2
- **Verified:** SR2 (§14.9.48.3, verified verbatim at specs/ISO_COBOL.md:32386): 'Identifier-1, identifier-2, identifier-3, and identifier-5 shall reference data items of category alphanumeric or national.' A category-numeric sender (PIC 9(5), usage DISPLAY) is neither alphanumeric nor national and is therefore NOT a permitted UNSTRING sender. CODE: the sender guard at StringUnstringBinder.cs:94 is `if (source.Item.Pic is { Category: PicCategory.Numeric, Usage: not Usage.Display }) return BoundUnsupported(…)` — it only rejects a numeric sender whose usage is NOT display, so a usage-DISPLAY numeric item passes. The statement then binds and emits: the source is rendered by ReadImage (StringEmitter.cs:71) = OperandText.AsString, whose own doc-comment (StringEmitter.cs:136-139) confirms it yields 'a numeric-DISPLAY item's sign-carrying zoned image', which UnstringExtract then examines. The guard's OWN message says 'category alphanumeric required', showing the intent WAS to enforce SR2 — the bug is the incomplete `Usage: not Usage.Display` qualifier. §4.2.2 obliges the implementation to be able to flag this SR violation. VERDICT CONFIRMED. Same-rule siblings also slip through and should be fixed with the pattern (feedback_scan_all_similar): a numeric-EDITED sender (category NumericEdited) and a boolean sender both bypass the :94 guard entirely; a fixed-length GROUP sender is treated as alphanumeric and IS permitted (SR10 forbids only a variable-length group), so it must remain accepted.
- **Fix:** StringUnstringBinder.cs:94-96 — the essential change is to reject ALL category-numeric senders (drop the `Usage: not Usage.Display` qualifier). Completing the SR2 pattern (numeric-edited + boolean) is the same rule:

    // SR2 — identifier-1 (the sender) shall be category alphanumeric or national (a fixed-length group is
    // treated as alphanumeric and remains permitted). A numeric item — INCLUDING usage DISPLAY, whose
    // zoned image would otherwise be examined as characters — a numeric-edited item, or a boolean item is
    // not a permitted sender.
    if (source.Item.Pic is { Category: PicCategory.Numeric or PicCategory.NumericEdited or PicCategory.Boolean })
        return new BoundUnsupported(
            $"UNSTRING sender '{un.dataReference().GetText()}' is category " +
            $"{source.Item.Pic.Category} (category alphanumeric or national required, ISO §14.9.48.3 SR2)");

Mechanism: BoundUnsupported ⇒ runtime-loud NotImplemented.Run, matching the STRING side and the existing UNSTRING SR convention (no silent execution). The CONFIRMED core is removing the DISPLAY exemption on the Numeric arm; adding NumericEdited/Boolean closes the adjacent SR2 gaps. Ship a Conformance test in the same commit. Same §4.2.2 note as CA19 re: runtime-loud vs. compile-diagnostic being a broader, joint STRING+UNSTRING decision.
- **Golden (spec-derived):**        IDENTIFICATION DIVISION.
       PROGRAM-ID. UNSTR-SR2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 SRC PIC 9(5) VALUE 12345.
       01 R1  PIC X(5).
       PROCEDURE DIVISION.
           UNSTRING SRC DELIMITED BY SIZE INTO R1.
           DISPLAY R1.
           STOP RUN.

Derivation: SRC is PIC 9(5) usage DISPLAY = category NUMERIC. SR2 requires identifier-1 be category alphanumeric or national; category numeric is excluded ⇒ the statement violates SR2 ⇒ a conforming processor must indicate the violation (§4.2.2). EXPECTED (spec-correct): the UNSTRING is rejected with a diagnostic naming §14.9.48.3 SR2 (codebase convention: a BoundUnsupported ⇒ NotImplementedCobolFeatureException naming SR2 — the digit image is NOT silently copied). CURRENT (buggy): compiles and runs; the zoned image '12345' is examined DELIMITED BY SIZE and moved into R1, so DISPLAY R1 prints '12345'. Editions: ALL (85/2002/2014/2023) — SR2 is edition-invariant for the COBOL-85 UNSTRING verb.
