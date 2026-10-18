## The skills pin moves to brent-tools 1.21.0

The public skills repo (BrentRector/claude-skills) is released as v1.21.0 (`b57b900`), with the push approved by the
owner on its exact text. It generalizes the 2026-10-17/18 lessons, each labelled practice, not yet validated:
- failure-path tests validate the design, and a missing one is a review finding (`engineering-standards` §5,
  `review` Step 5);
- run a concurrent or crash-recovery design against a fault-injecting model from the first review round;
- a successor chain carries each link's reviewed head forward (`agent-fleet` §7, `references/grouping-fixes.md`);
- the orchestrator loop's lessons 11 and 12: a standing constraint carried as prose is dropped, and one orchestrator
  per branch.

`tools/claude-skills` moves to tag `v1.21.0`. The five overlays that name the pin (architecture-review, gate,
review, spec-lookup, workstream) say 1.21.0, and kb/Work PB1699 records the version and the pin. The project-side
counterparts (MANDATORY-PRACTICES I11, notes PB3378-PB3380) land on their own branch.
