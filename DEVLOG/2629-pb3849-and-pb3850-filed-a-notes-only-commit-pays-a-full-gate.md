## Entry 2629 — 2026-10-26 15:16 PDT — PB3849 and PB3850 filed: a notes-only commit pays a full gate and review, and the work-note merge keeps both sides of a replaced line

The owner, on PB3837's merge (15:12, relayed by the Mythos lane): "20 minutes for a stupid one line change". Two
defects, filed as their own cluster (PB3849, also PB1957 tier 1) to be planned first once PB3837 lands.

PB3849: `prune_worktrees.mechanical`, the one coverage rule the review gate and the landing read, exempts only
DEVLOG-only commits, so a commit that only deletes a stale line in a kb/Work note after a reviewed, gated head costs a
new leg-1 gate and a new branch review. Such a commit should be covered by `work.py check` and the audits.

PB3850: the work-note merge driver merges a note's body by git's union merge, so a line main replaced while the branch
also changed it is kept twice. PB3837's merge kept kb/Work/PB3830.md's old Sites line beside main's new one, and only the
branch review caught it; the Mythos lane's mechanical check (each merged file equals the rename of main's file: 102 of
103) belongs in the conflict finisher's exit check.

Rows re-verdicted: none. No code changed.

