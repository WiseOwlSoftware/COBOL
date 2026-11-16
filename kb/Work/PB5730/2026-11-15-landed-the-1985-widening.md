## Landed: one widening population, and the 1985 widening batch (2026-11-15, wave 20262051 group A)

**The tool.** `lineage_reads.unwitnessed` is `widen`'s one population: the stored KEPT records with no own test-ref
(`bare_kept`, which `witness-batch` now shares) and the read decisions refused only for `no own-edition witness`.
`widen_plan`'s classification is unchanged; each `needs-own-witness` row now says why per lacking target (`REASON_OF`:
no-case, band, not-evidence, diagnosis-only, no-edition, uncommitted), the counts give rows by reason, and a stored row
the search already witnesses is `witness-batch`. `--state` is needed only where read records exist (`widen_reads`).
Proof of no change at 2002: the plan from `E:/COBOL-coord/scratch/records-2002-u1/state` is identical before and after
(138 rows: 117 needs-own-witness, 21 widenable; same cases, same counts). At 1985 the plan is the prototype's: 803 rows,
483 widenable, 320 needs-own-witness, every state and case status equal.

**A second defect found on the way, fixed at its root.** The in-memory proof (`Witnesses.entries`) ran
`generate.check_cases`, but the free-form and run-time-expectation refusals lived in `Reader._check_program`, which runs
only on a file read from disk. The first run proved and wrote 33 free-form cases at 85 that `generate.py --check` then
refused. Both checks moved into `generate.check_editions`, called from `check_cases`; the first run's writes were
discarded and the batch run again (scratch `w20262051a/widen-run2`).

**The batch.** 365 candidate cases (322 widen, 43 widen-refusal); 74 proven at 85 and written, 291 not proven: 258
later-construct (COBOL0900 153 and the other edition gates; 180 of them the generator's free form at 85), 33 harness (a
run-time expectation accepted at 85, which the suite judges only from 2002), 0 defect-suspected
(`E:/COBOL-coord/scratch/my-20261115/gl1985/widen-failures-1985.json`). `witness-batch` then wrote 64 records
(+120 test-refs): **1985 835 to 771 GAP**; a second witness-batch writes 0; `lineage_reads.py --check` GREEN (1985: 634 of
1,373 KEPT records with their own witness). The 419 widenable rows whose widening did not hold are the witness lane's
later-wave input (`E:/COBOL-coord/scratch/my-20261115/gl1985/remaining-1985.json`, 288 with no target witnessed, 131
partial).

**Stopped: the witness-about-the-row predicate.** `lineage.unrelated_witnesses` (a KEPT row's own test-ref names a case
citing the row's rule or a target) was built for the audit and the writer, and 25 stored references fail it (10 at
1985, 15 at 2002): the 2023 rows' own test-refs name cases whose headers do not cite them, and witness-batch inherits
them. Per the instruction it was committed (f3d4a58c1e) and reverted (f478456ac9) before any record changed.
