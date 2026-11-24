## The owner releases the "no known compiler defects" hold: R2's remaining batches and the Delete program start

**Owner, 2026-11-24 (account 1):** "I release the 'no known compiler defects' hold". Asked whether that also lifts the
Delete program's own hold of 2026-11-15 (zero GAP in 2002 and 1985 first), the owner answered "Yes, start Delete too".

**What changed.**

- `kb/Work/PB2911/2026-11-24-owner-releases-the-defect-hold.md` records the release. It also records what R2 actually
  covered: the run pinned at cd85e49017 reached batches 1, 2, 3, 4a, 4b and 5b, every one COMPLETE, 38 of its 75 shards. The
  other 37 shards (binding, the bound tree, code generation, `oo`, and tests-scripts-ci 8 to 30), about 216,000 of 383,000
  lines, are unreviewed.
- R2 continues on a NEW pin at the main of the day each batch launches, not at the old pin. The projects were renamed and
  files moved since cd85e49017, and the filer refuses any path the tree no longer has.
- `kb/Work/PB2119.md` goes from `blocked` to `open`, with `2026-11-24-owner-starts-the-program.md` beside it. Each of the 126
  open Delete notes is re-measured against the main of its day before anything is removed.
- Plan §0's PRIORITY line, DESIGN-architecture-review §8.4 (PB2350 no longer waits on the hold) and
  DESIGN-conformance-suite §1 (only the zero-GAP condition remains) now describe the released state.

**What did not change.** The quota plan PB5357 (no borrowed days, day lines; R2 batches are sized against the room
`budget.py` reports) and the rule that every Mythos or Fable dispatch needs the owner's approval.
