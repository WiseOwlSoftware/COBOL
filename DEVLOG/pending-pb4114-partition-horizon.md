## The 32-partition change's review leads are recorded, and the landing lane's missing hold is filed (PB4145)

The branch review of the 32-partition change (tooling-suite-partitions) left three leads. Two of them were filed as
kb/Work PB4114:
- Nothing warns before main plus a branch crosses `Partitions × 500` rows again. At 32 partitions that is 16,000 rows,
  and only the merge queue can meet the crossing.
- The 32-partition Conformance leg was not re-measured.

The owner approved a redesign the same afternoon, kb/Work PB4146 in the Mythos lane: derive the count from the row total
and guard balance, not size. PB4114 is retired into it. The third lead, dated history in current-state text, is fixed here
in `DESIGN-conformance-suite.md`. The two partition summaries in the test files are left for PB4146, which rewrites
those files.

kb/Work PB4145 records what the crossing showed about the landing lane. A merge-group red that every later group will
meet (here the partition bound) does not hold the lane. It went on queueing pull requests whose groups were known to
fail, until the operator dequeued them, cancelled their runs and held the lane by hand. The only hold the operator had
was `HALT`, whose recorded cause is QUEUE NOT ACTIVE.
