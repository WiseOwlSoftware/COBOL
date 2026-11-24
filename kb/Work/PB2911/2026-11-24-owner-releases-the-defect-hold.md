## 2026-11-24 - the owner releases the "no known compiler defects" hold

**The owner, 2026-11-24 (account 1 session):** "I release the 'no known compiler defects' hold".

That hold (this note, 2026-10-10) kept two pieces of the architecture review (PB1754) waiting until the compiler had no known
defects:

1. **R2's remaining review batches.** The plan still called them "batch 3". In fact the run pinned at cd85e49017 went on to
   batches 3, 4a, 4b and 5b, and each one reads COMPLETE under `r2_collect.py`. The batches covered 38 of its 75 shards. The
   other 37 shards, about 216,000 of 383,000 lines at that pin, are still unreviewed: data binding 1, 3, 5 and 6, procedure
   binding 1 to 4, the bound tree, code generation 1 to 4, `oo`, and tests-scripts-ci 8 to 30.
2. **The Delete program (PB2119).** Asked the same day whether the release also lifts that program's own hold (owner,
   2026-11-15: wait until the 2002 and 1985 inventories read zero GAP), the owner answered "Yes, start Delete too". See
   `kb/Work/PB2119/2026-11-24-owner-starts-the-program.md`.

Lifting the hold also frees the other things that waited on it:

- **PB2350**, the grammar differential. Since 2026-11-03 it had waited on this hold, as DESIGN-architecture-review §8.4
  recorded.
- **The conformance suite's clause-by-clause completion.** Its "zero known defects" condition is gone. Its zero-GAP condition
  still holds (DESIGN-conformance-suite §1).

**What does not change:**

- The quota plan in force (kb/Work PB5357): no borrowed days. Each account keeps to its day line, and `r2_inputs.py --batch`
  and `r2_collect.py --launch` refuse a batch that cannot finish in the room `budget.py` reports.
- Every R2 reviewer is Opus. No Mythos or Fable dispatch follows from this release without the owner's approval of that
  dispatch.

**How the rest of R2 runs (Claude's decision, CLAUDE.md Ownership):** the next batches are taken on a NEW pin at the main of
the day they launch, not at cd85e49017. Seven weeks of landings since that pin renamed the projects (to
`WiseOwl.Cobol.*`) and moved many files, and `r2_collect.py --r2` refuses any path the committed tree no longer has. So a
finding against the old pin would be stale before anyone filed it.

The new pin's census and shard map come from `census.py` and `r2_subsystems.py --out`. The batches cover the subsystems the
old run never reached, and small shards go first (§3 R2, "Running a batch"). Every batch is sized by `r2_inputs.py --batch`
against account 1's room.

This note stays `open`, because it leads the GAP campaign, which is unfinished: 2002 still reads 1,147 GAP.
