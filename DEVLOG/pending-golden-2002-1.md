## The 2002 golden lane, first landing: 201 witnesses at --std 2002 for 24 of 85 files, 2002 GAP 2,562 to 2,340

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
- PB5621 (extended): an ANY LENGTH formal bound BY REFERENCE to an ODO group takes the group's maximum length (2002-GR-8.8.4.1.3.3-1, 2002-GR-13.16.2.3-1).
- PB5577 (extended): the VALIDATE decline COBOL1708 hides SR18 and the validation half of level 88 (2002-SR-13.14.2-18, 2002-GR-13.16.31.3-2).
- PB5606 (a dated finding): SAME AS a COMMUNICATION SECTION record is accepted (2002-SR-13.16.47.2-7, already claimed).

The writers held 2002-SR-14.8.47.2-12 to -15 on the WRITE FILE decline. PB5579 has since landed, and `WRITE FILE` now
compiles at 2002, so those rows wait only for a witness of their FILE arm.

**Not closable by a golden (9 rows).** 2002-GR-A.2.2.3-2: DISABLE's in-transmission guarantee has no observable
on this MCS. 2002-GR-12.3.4.8.3-2: the processor supports record locking, so the rule's antecedent is empty.
2002-GR-14.8.23.3-10 and -11: user constraints whose violation is undefined (§2002:3.3). 2002-GR-7.2.14.3-1: an implied
LEAP-SECOND OFF cannot be told from ON. 2002-GR-7.2.15.2-2 to -5: no source listing is produced. Each needs a 2002
derivation or the registrar's decision, not a test. The writers deferred nothing.

**Gate and oracle.** These are in the report of golden-2002-1.
