## PB5612: a SORT or MERGE's own fatal condition is the verb's to dispose of only from 2023

The 2002 read found that at `--std 2002` a fatal EC-SORT-MERGE-FILE-OPEN on a MERGE, whose declarative completes normally, let
the run unit go on. The re-probe reproduced it at 2002 and, because §2014:14.6.12.1.2 reads like §2002:14.5.12.1.2, at 2014:
`DECL EC-SORT-MERGE-FILE-OPEN`, then the statement after the MERGE, rc 0. The cause is the rule the SORT/MERGE guard applied at
every edition. §2023:14.6.13.1.3 2) ("If the executed statement is a MERGE or SORT statement, then the rules for those
statements apply") is new in 2023; the two older fatal-exception procedures have no such item, so a checked fatal condition
runs the declarative and then "the execution of the run unit is terminated abnormally" (item 2), propagates under
`>>PROPAGATE ON` (item 3), or terminates (item 4).

**Fix.** One dialect behaviour, `DialectBehavior.SortMergeStatementDisposesItsFatalConditions` (changed in 2023, VCR row 7.80),
asked through one predicate, `EcEmitter.SortMergeDisposesFatalConditions`, by both arms of the old rule: the SORT/MERGE guard
(`EcEmitter.VerbDisposes`, which now takes the edition) and the implicit-transfer hooks (`SortEmitter.EmitTransferUse`, whose
`verbDisposes` switches `__IoCheckEc`'s `__verbRule`). The sweep named in the note is covered by the same predicate: a SORT's
file-open, the fatal EC-I-O of an implicit OPEN, the nested SORT's EC-SORT-MERGE-ACTIVE and automatic propagation (a fatal
status of a SORT in a `>>PROPAGATE ON` program was swallowed and now reaches the caller). The runtime mark is unchanged.
D-SMA (b) and VCR row 7.80 carry both editions' texts.

**Goldens.** Four per-edition tests with a `.out` (2023) and a `.2014.out`: `pb5612_merge_file_open_fatal_by_edition`,
`pb5612_sort_file_open_fatal_by_edition`, `pb5612_sort_io_status_fatal_by_edition` (fatal status 30, checking on, no
declarative) and `pb5612_sort_fatal_io_propagates_by_edition`; plus the two 2002-rule twins that carry the rows' own ids.
`pb1036` became a 2023 witness; `pb993` lost leg 9 (now `pb5612_sort_io_status_fatal_by_edition`) and moved to `14.9.40-SORT/`.
Unit and conformance tests: the predicate by edition and the nested-SORT disposition below 2023.

**Rows.** 2002-GR-14.8.36.3-8 and 2002-GR-7.2.17.3-1 closed (2002 GAP 2497 to 2495). GR-14.6.13.1.3-2 now says what the text
says: it exists in 2023 alone (it had claimed 2002 and 2014). The pb993 witness of five rows moved with the golden.

**Self-review.** Architecture: one predicate, no per-site edition test, the drift test of the transfer hooks reads it.
Full code: the arms agree (guard, `__IoCheckEc`, uncovered-fatal shape); item 8's rollback is moot below 2023 (APPLY COMMIT is
2023-only). Performance: compile-time only. Duplication: the 2002 twins repeat a program because an older-edition rule id
cannot share a test with newer ones.

**Review round 1 (R1-1).** `EmitUseHook`'s one `verbDisposes` flag had been turned off below 2023 and so also lost the deferral of the uncovered-fatal termination to the SORT's disposition: at 2002/2014 the GIVING WRITE terminated the run unit before the boundary break, with the name SORT/MERGE. Split into `sortMergeTransfer` (all editions: defers the termination, item 8 off) and `verbDisposes` (2023 only: `__verbRule`). Test: `SortMergeStatementEcTests.TheGivingWriteBoundary_PrecedesTheUncoveredTermination_AtEveryEdition` (2002, 2014, 2023).
