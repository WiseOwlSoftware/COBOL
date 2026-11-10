## Entry 3443 — 2026-11-10 12:07 PST — PB4995 item 3: 709 checked 1985 drafts placed as conformance tests of their own rules; PB5021 is the owner's question

**Wave 20261977, group B (PB5021, PB4995, PB4996).** The owner approved on 2026-11-09 the Mythos confirmation pass of the
1985 MATCH drafts and the placement of its witnesses. The pass left 767 records under
`E:/COBOL-coord/scratch/confirm-85/out`: CONFIRMED 473, WEAK or WRONG with a MATCH redraft 243, a MISMATCH redraft 30 (the
filed defects PB5101-PB5108), UNDECIDED 21.

**The suite could not hold a 1985 rule's test.** The generator knew only the 2023 inventory: a test naming
`1985-GR-VI.6.18.4-3` was refused as an unknown rule, and no folder could hold a 1985 clause. It now reads every held
edition's rules (`generate.Inputs.older`). An older rule's band is its own edition alone (`Inputs.band`; its successor
at a later edition is another rule reached through the change record), and its test sits in the folder of its own
clause, qualified by the year, `cases/VI.6.18@1985-INSPECT-STATEMENT/` (`CLAUSE_FOLDER` gained the roman numeral and the
`@<year>`; `check_cases` holds a rule to a folder of its own edition). The completeness report still counts the newest
edition's rows; an older edition's are its own view's. The self-test plants a 1985 rule: accepted at 85, refused at 2002,
refused in a 2023 folder, and a 2023 rule refused in the 1985 folder.

**One script does the placement, `scripts/spec/place_witnesses.py`.** It reads the records (a CONFIRMED rule takes the
audit run's own draft, read with `audit_batch.answers_by_id`; a WEAK or WRONG one its MATCH redraft) and judges each
witness again on this build through the harness (`run_drafts.harness_run`, extracted so the runner and the placement
build one harness run, and `run_drafts.judge`). An output draft becomes a test accepted at 85 with its `.out`. A refused
draft becomes a WiseOwl refusal at 85 pinning the code the compiler reports at the draft's violating line, read with the
adapter's own diagnostic pattern, which gained `severity` and `code` groups (ConformanceSuiteAdapterDriftTests now also
holds the code it reads to every code the compiler reports). The flagged form waits for a clause pass, because a draft's
control has no expected output. Every placed test is `portable: unverified`: a model's draft was checked against its rule,
never against the portability lint or the profiles' `requires`. The self-test plants a CONFIRMED output, a WEAK refusal
redraft, an UNDECIDED record, a COPY draft, a warning-only refusal and a MISMATCH redraft through the stub compiler.

**Result on main b91ec90f41:** 709 placed (571 output, 138 refusals). Skipped 7: one COPYs a library (PB5067). One redraft
is now refused for another rule: `1985-GR-XIII.4.5.4-12`, OPEN EXTEND of a MULTIPLE FILE TAPE file, COBOL6814, a suspect
draft. Five draw only a warning at their violating line (the USE statements' GR2 in four modules, and
`1985-SR-XV.3.2.3-2`). `generate.py --check` is GREEN at 7,469 tests.

**Recorded:** 556 witness-only `test-ref`s on the CHANGED (361) and REMOVED (195) rows, through `record_verdicts.py
--edition 1985`. A witness-only record on an older row is now accepted when its change record is its own and no verdict
is read yet (`validate`; the reference check is one helper, `_reference_errors`). The 1985 GAP stays 1,046: CONFORMS
requires the `code-location` a reader of the compiler names, and no script derives one from a passing test. 141 placed
rules sit on KEPT records (their test is their chain's) and 12 have no change record yet. Neither group is recorded. The
21 UNDECIDED records and their questions are listed in PB4995.

**PB5021 is the owner's question.** Neither section 10.2's table nor the prompts say whether a 2023 case reachable only
through syntax 1985 lacks makes a rule CHANGED, and the note's own Cause and Expected answer it oppositely. The placement
does not wait on it: a test names the 1985 rule whatever its kind, and rerunning the script re-records what the answer
reclassifies. **PB4996** re-probed: band 85 drift 113 unstored, 931 unproven, 14 unclassified, so the stored `85` stays.

**A second placement, the same script (attended Mythos session, after the wave).** The first run took only the confirmation
pass's records. Two more sets of checked 1985 witnesses existed: the 117 suspect drafts the triage of the non-matching drafts
corrected and re-ran (E:/COBOL-coord/scratch/triage-85/out), and the 55 witnesses the drafting pass wrote for rules the audit
never drafted (E:/COBOL-coord/scratch/draft-85/out; its 18 defects are PB5115-PB5130). `place_witnesses.py` gained a third
record kind for them, DRAFTED: a record that names the pass that wrote the witness and carries the draft and its MATCH run
(self-test: a planted DRAFTED record is placed and recorded, its test naming the pass). Result on the tree with main merged:
157 placed (98 output, 59 refusals), 143 recorded, 13 their chain's, 15 skipped (each COPYs a library, PB5067). In all 866
1985 witnesses are placed and 699 rows carry a `test-ref`; the 1985 GAP is still 1,046, because each of those rows now
needs its code read and its verdict.

