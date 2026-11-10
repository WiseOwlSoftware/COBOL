## Entry 3418 — 2026-11-10 00:22 PST — PB5099 filed: a re-gate at an unchanged tree should re-run only what did not pass

The owner saw wave 20261972's single group spend three hours gating one 13-minute change and ruled: "If only one test failed,
any retry should test only that one case." The records bear it out: run 000002 of that worktree passed 22,783 Conformance,
40,729 Unit and 37 Characterization cases and failed one on a harness non-observation under host load; run 000003 at the same
tree (fc15c9ace5) waited 24 minutes for a slot and re-ran all 63,000 cases for 58 minutes to clear it. THE RULE already asks
only that every case ran green at the pushed head, so a re-gate at an unchanged tree and toolchain can compose with the earlier
run's passes and re-run only its reds, non-observations and unrun cases. PB5099 records the decision and what gate.py,
enqueue.py and the gate skill owe for it.

The operator added two things on the same branch. PB5099 gains Mythos's sibling case: a commit that changes only inert paths on top of a GREEN code-lane run also re-runs the whole population, and it should compose the same way. PB5100 is filed: the rolling wave starts 6 implementers against a gate pool of 3 slots (C waited 1,601 s, PB5060's re-gate 1,448 s), and its concurrency should be derived from the pool, not hand-set.

