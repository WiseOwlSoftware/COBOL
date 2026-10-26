## Two process defects filed: a mid-merge worktree crashes the planner (PB3786), and a sparse-checkout CI job can lose an import silently (PB3787)

PB3786: `plan_wave.py --cluster PB3726 --dry-run` died with WinError 206 in `inflight_units` while a finisher was merging
main into worktree-wf_6e0a917c-500-26 (3,331 changed paths): the uncommitted arm splats every shared path onto the `git
diff` command line with no `PATHSPEC_CHARS` guard, though the committed arm beside it has one. Tier 1 of PB1957.

PB3787: branch review lead R2-1 of w20261503a. No drift test reads the `supersede` job's sparse-checkout list, so a new
repository import in cancel_superseded.py, queue_facts.py or ruleset.py makes the job cancel nothing behind
continue-on-error while CI stays green.
