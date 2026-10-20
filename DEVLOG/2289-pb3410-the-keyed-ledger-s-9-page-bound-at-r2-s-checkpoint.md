## Entry 2289 — 2026-10-20 11:05 PDT — PB3410: the keyed ledger's 9-page bound at R2's checkpoint has a witness

DESIGN-runtime-library.md 2.2.1.11 says the ledger's path copy ℓ grows with the ledger's fill, "at most 9 pages at R2's
checkpoint (1 024 entries plus the pages of the one statement that begins at it)". `KeyedStorePerformanceWitnessTests.Load`
computed `mostLedgerPages` for every statement and printed it, but asserted only the per-statement row (with ℓ read from the
file) and the entry count. A ledger layout change that let ℓ reach 20 pages at the checkpoint would have stayed green while
the row's number went false.

**Re-probe.** On the branch build the twelve load cells (three shapes, both orders, both sharing modes) report the ledger at
most 1 028 entries on 9 pages (IndexedTwoAlternates, the largest), 5 to 6 pages for the single-key shapes' random loads, and 1
for the ascending ones: the doc's number is the measured maximum, true and unasserted.

**Fix.** The test names the bound (`LedgerPagesAtCheckpoint = 9`) and `Load` asserts `mostLedgerPages` against it for every
statement of every cell. The sibling arm, the OPEN row's "3 + the ledger tree's pages" read after a process death, asserts the
ledger tree it reads against the same constant. The `Load` summary (which restated the checkpoint bound without the page count,
the PB3417 fold-in) states both. The design paragraph names the witness and the measured maximum, so the number cannot drift
from its assertion unseen.

**Mutation check.** With the constant at 8, two of the twelve load cells fail (`Assert.InRange() Failure`, `Actual: 9`); at 9
all pass.

No inventory row was claimed or closed (`closes_rows: []` with its reason). No production code changed. Self-review (silent
failure, test analyzer, comment analyzer) found nothing further.

