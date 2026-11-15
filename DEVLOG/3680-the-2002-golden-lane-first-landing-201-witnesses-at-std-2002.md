## Entry 3680 — 2026-11-15 05:51 PST — The 2002 golden lane, first landing: 201 witnesses at --std 2002 for 24 of 85 files, 2002 GAP 2,562 to 2,340

The 2002 golden lane (gl2002, kb/Work PB5597; DESIGN-spec-conformance-review.md section 10.6) gives each 2002 row that
the 2002 read left needing evidence a witness of its own at `--std 2002`. It covers 622 rows in 85 input files:
needs-own-witness 408, refused-evidence 124, refused-needs-accepting-case 35 and own-verdict-from-pointer 55. Opus
writers drafted 24 of the files, and refuters and fixers checked them. This landing integrates those 24 files, 246 rows
in all.

**Dispositions.** There are 216 new-golden rows, 8 existing-golden, 13 defect-suspected and 9 not-closable. By work
kind, needs-own-witness went 169 new and 2 existing golden, 11 defect-suspected and 1 not-closable; own-verdict-from-pointer
went 15 new and 6 existing golden, with 2 defect-suspected; refused-evidence went 22 new golden and 8 not-closable;
refused-needs-accepting-case went 10 new golden. The refuters overturned 34 verdicts: 12 drafts did not exercise the
rule, 10 had a citation wrong, 4 were not spec-derived, 4 broke a convention, 3 had a wrong expected value and 1 used
the wrong kind of evidence. A fixer repaired every one, and nothing a refuter overturned lands unrepaired.

**Validation.** No separate validation stage ran, so the integrator validated every draft on a build of main b15bb9088f
before it landed. The 91 accepted cases ran through `conformance-suite/harness/run.py` at 2002 on a manifest of exactly
those cases: `PASS 91 · FAIL 0`. The 110 refusals were compiled at `--std 2002`, and each was refused with its expected
code: 110 of 110. The integrator made three mechanical repairs: three refusal headers had no `source-format` (the
generator refused them), and three PROGRAM-IDs collided between files (G2OCC01, G2SGN01 and G2VAL01 were renamed
G2OCC02, G2SGN02 and G2VAL06). No expectation was changed. Four drafts were held back under
`E:/COBOL-coord/scratch/gl2002/lane1/out/` because only defect-suspected rows use them: `g2_write_random_lock_phrase_manual`,
`g2_write_random_lock_phrase_automatic_neg` (and its refusal), `g2_class_zero_length` and `g2-lvl-cd-special-levels`.

**Records.** `record_verdicts.py --edition 2002` wrote 222 CONFORMS records, and the GAP went from 2,562 to 2,340. Each
test-ref was checked mechanically against the generated wiseowl manifest of this tree: every case it names is accepted
at 2002 or is a refusal whose `refuse-at` holds 2002. A refusal-only record was admitted only for an SR or FMT rule
whose text grants no form (`lineage-evidence-kinds.json`). Three records were dropped. The first,
2002-GR-13.16.38.4-13, is a defect-suspected row. The other two, 2002-GR-13.16.38.4-14 and 2002-GR-13.16.2.3-1, have
goldens that pass and land, but another file's repro breaks the same rule on this build: PB5650 for the first, PB5621
for the second. No row was widen-existing, so no refused-evidence row could have been KEPT through a widened pointer
case.

**Suspected compiler defects** (each re-probed on this build). Twelve rows are recorded DIVERGES, naming their notes:
- PB5650 (new): a locale-edited picture with no currency symbol still gets the locale's sep_by_space space (2002-GR-13.16.38.4-13, -14).
- PB5651 (new): COBOL2021 treats a multiple-operand LINE clause as an OCCURS clause and refuses a legal OCCURS DEPENDING beneath it (2002-GR-13.16.51.3-4).
- PB5652 (new): an unprefixed REPOSITORY SUM intrinsic inside a report SUM clause is a parse error at 2002 (2002-SR-13.16.52.2-1).
- PB5602 (extended): a subscripted RECORD KEY is refused COBOL2024 (2002-GR-12.3.4.4.3-2; -3 was already claimed).
- PB5620 (extended): the 2002 Format 1 WRITE phrase order is a parse error, so SR21 cannot be witnessed (2002-SR-14.8.47.2-21).
- PB5621 (extended, since retired): an ANY LENGTH formal bound BY REFERENCE to an ODO group takes the group's maximum length (2002-GR-8.8.4.1.3.3-1, 2002-GR-13.16.2.3-1). That is the standard's rule; see the PB5621 merge below.
- PB5577 (extended): the VALIDATE decline COBOL1708 hides SR18 and the validation half of level 88 (2002-SR-13.14.2-18, 2002-GR-13.16.31.3-2).
- PB5606 (a dated finding): SAME AS a COMMUNICATION SECTION record is accepted (2002-SR-13.16.47.2-7, already claimed).

The writers held 2002-SR-14.8.47.2-12 to -15 on the WRITE FILE decline. PB5579 has since landed, and `WRITE FILE` now
compiles at 2002, so those rows wait only for a witness of their FILE arm.

**Not closable by a golden (9 rows).** 2002-GR-A.2.2.3-2: DISABLE's in-transmission guarantee has no observable
on this MCS. 2002-GR-12.3.4.8.3-2: the processor supports record locking, so the rule's antecedent is empty.
2002-GR-14.8.23.3-10 and -11: user constraints whose violation is undefined (§2002:3.3). 2002-GR-7.2.14.3-1: an implied
LEAP-SECOND OFF cannot be told from ON. 2002-GR-7.2.15.2-2 to -5: no source listing is produced. Each needs a 2002
derivation or the registrar's decision, not a test. The writers deferred nothing.

**Gate and oracle.** The first gate was RED on CITATIONS: in one golden's header comment, a `§2002:` citation was
split across two lines. Rejoining it fixed the audit. The second gate: `=== BUILD-LOCAL GATE: GREEN — Conformance
25,654/25,654 ran · Unit 40,812/40,812 ran · RepoDrift 175/175 ran · Characterization 37/37 ran` (run
000002-20261115T134333Z). Conformance.trx holds a Passed 2002 result for each of the 201 new cases: 90
`SuiteTest_IsNoWorseThanItsVerdictAllows` (the 91st under a truncated name) and 110
`WiseOwlRefusal_RefusesTheSourceWithItsDiagnostic`. `compare_oracle.py` reports `DIFFERENT — 201 of 20268 cases
differ`: every one is an ADDED line naming exactly the new cases at 2002, with no CSHARP, DIAGNOSTICS or REMOVED
line.

**Merged with PB5602 (queued ahead).** The queue would have removed this pull request for a merge conflict with
worktree-wf_7d5ef379-5e3-25 (kb/Work PB5602 landed), so its head was merged in. The one conflict, PB5602's
`inventory_rows`, keeps the landed list and this branch's 2002-GR-12.3.4.4.3-2. With PB5602's fix in, the two DIVERGES
records that named it were wrong: the repros `r2c_reckey_subscripted_seq.cob` and `r5_relkey_subscripted.cob` now print
`R:ZA`/`R:AB` and `R3=AAAA`/`INV1` at --std 2002. New golden `g2acc03_subscripted_key_access` (12.3.4.4@2002) witnesses
the subscripted arms of GR2 c) and GR3 a) and b): an indexed file written QC, ZA, MB reads back as ZA, MB, QC under a
RECORD KEY of IXS-K(2). `record_verdicts.py --edition 2002` re-recorded 2002-GR-12.3.4.4.3-2 and -3 CONFORMS, and GAP
went from 2,323 to 2,321 at the merged head (2,321 GAP, CONFORMS 275, DIVERGES 10). Both rows leave PB5602's `inventory_rows`.

**Merged with PB5620 (landed on main).** The queue bounced pull request #1710 DIRTY: main had landed PB5620, and the one
conflict was `kb/Work/PB5620.md`'s `inventory_rows`, which this branch had widened with 2002-SR-14.8.47.2-21 and main's
landing released. A landed note holds no claim, so main's frontmatter is taken. With PB5620's fix in, the DIVERGES
record of -21 was the parser's refusal, not SR21's: at --std 2002 the MANUAL Format 1 repro now prints `W 00 N=2` and its
AUTOMATIC twin is refused COBOL5993 at the WITH LOCK. Four goldens (14.8.47@2002) witness SR21 on both formats: the
Format 1 pair `g2_write_sequential_lock_phrase_manual` / `_automatic_neg` (the 2002 phrase order), and the Format 2 pair
`g2_write_random_lock_phrase_manual` / `_automatic_neg` the writers had held until PB5620 landed. `record_verdicts.py
--edition 2002` re-recorded -21 CONFORMS; at the merged head the 2002 inventory is 2,282 GAP, CONFORMS 314, DIVERGES 9,
PARTIAL 1.

**Merged with PB5621's retirement (landed on main).** The queue bounced pull request #1710 DIRTY again. Main had retired
PB5621, and the one conflict was `kb/Work/PB5621.md`'s `inventory_rows`: this branch had added 2002-GR-8.8.4.1.3.3-1 and
2002-GR-13.16.2.3-1 to it, and main emptied it. A retired note claims no rows, so main's frontmatter is taken. Main's reason
for retiring the note also removes this branch's two DIVERGES records: the repro passed the ODO group BY REFERENCE, and
§2002:14.7.1.1 gives such an argument its maximum length. So the formal has length 5, is not zero-length, and five spaces
are ALPHABETIC; the output was correct. 2002-GR-13.16.2.3-1 is CONFORMS on the BY CONTENT golden already landed here,
`g2-any-length-zero-length-by-content`, and on main's `pb5621-unstring-any-length-variable-occurrence-2002`, whose BY
REFERENCE leg pins the maximum. The held draft `g2_class_zero_length` (8.8.4.1.3@2002) now passes its argument BY CONTENT
and witnesses 2002-GR-8.8.4.1.3.3-1. At --std 2002 it prints its expected output, and the same source BY REFERENCE prints
`S0-ALPHA=T`, so the leg can fail. `record_verdicts.py --edition 2002` re-recorded both rows CONFORMS. At the merged head
the 2002 inventory is 2,277 GAP, CONFORMS 319, DIVERGES 7, PARTIAL 1.
