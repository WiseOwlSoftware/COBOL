**2026-11-16, the conflict finisher of worktree-wf_f60b9cb3-27d-6: the four rows main added are closed by this fix.**

Main added `2002-GR-14.8.36.3-9`, `2002-GR-14.8.23.3-7`, `2002-GR-14.8.23.3-8` and `2002-GR-14.8.23.3-12` to this note's
`inventory_rows` (`2026-11-15-golden-2002-3-nested-sort.md`, `2026-11-15-read-2002-d.md`) while the fix waited to land. On the
merged build (`cobol.exe <repro> --std 2002 --run`): `g9.cob` prints `BEFORE-INNER` and then the abnormal termination for
EC-SORT-MERGE-ACTIVE; `merge-in-op.cob` prints `IN OP 0001` and then the same; `merge-open-USING.cob` and `merge-open-GIVING.cob`
print `DECL=EC-SORT-MERGE-FILE-OPEN` and then the abnormal termination (exit 1 each). At `--std 2023` the GIVING repro still prints
`AFTER MERGE` (§2023:14.6.13.1.3 2)). The one predicate `EcEmitter.SortMergeDisposesFatalConditions` covers them all.

Witnesses (accept [2002]): `14.8.23@2002-MERGE-STATEMENT/pb5612_merge_using_open_fatal_2002`, `pb5612_merge_giving_open_fatal_2002`,
`pb5612_merge_in_output_procedure_fatal_2002`, `14.8.36@2002-SORT-STATEMENT/pb5612_nested_sort_in_input_procedure_fatal_2002`.
Recorded CONFORMS through `record_verdicts.py --edition 2002` (2002-GR-14.8.36.3-9 DIVERGES to CONFORMS); the six rows are this
note's `closes_rows`.

**The merge's one red (LINEAGE READS).** Main's KEPT record `2002-GR-14.8.23.3-1` (PB5675) named pb993 at its old folder,
`14.6.13.1.3-Fatal-exception-conditions/`, which this branch moved to `14.9.40-SORT/`. It was re-cited through
`record_verdicts.py --edition 2002` with a `retire-witnesses` of the old path; `lineage_reads.py --check` is GREEN. No other
record names the old path (grep outside the retired-witness marks and the DEVLOG: nothing).
