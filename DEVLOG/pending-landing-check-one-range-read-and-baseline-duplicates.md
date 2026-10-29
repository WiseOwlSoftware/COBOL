## The landing check reads its range once (PB3791) and stops a public-API symbol listed twice (PB3694)

Wave 20261752 group B, two notes with one root: `scripts/orchestrator/landing_check.py` held a second copy of a question
and lacked a check the merge queue makes for it.

**PB3791.** `check()` asked git for the changed files of `base...rev` twice, through `inflight.changed_files` (for the
partition's `mine`) and in arm 0's own `git diff --name-only` (for the paths no landing carries). The two differ only in a
filter: `changed_files` drops the NEVER_WORK paths arm 0 must see. The root is one question and two filters:
`inflight.range_names` now asks `git diff --name-only --no-renames <revs>` and returns every path, `changed_files` is
`work_names(range_names(...))`, and `check()` reads `range_names` once and derives `mine` and the leaked paths from it. The
self-test counts the range's `diff --name-only` through a counting git callable for a landing with and without a NEVER_WORK
path; re-adding a second read fails both arms (checked by mutation). `test_plan_wave.py` pins `range_names` against
`changed_files`. One fewer git process per landing.

**PB3694.** Re-probe: base `A.A C.C E.E`, main adds `X.X` after `A.A`, the branch adds it after `E.E`. The union merge keeps
`X.X` twice, and so does git's plain line merge (exit 0), which is what GitHub's merge group runs because no driver is defined
there (PB3642); the registry's `merge_symbol_set` (PB3727) makes one line. So PB3727 already closed the local half, and the
case that bounced PR #541 with RS0025 could still come back through the queue's driverless merge with nothing local reading
it. The rest of the note's fix direction is now in: `merge_drivers.baseline_duplicates` (a symbol twice in one file, or one
plain line in two files of one directory, which is how the analyzer counts RS0025; `*REMOVED*X` is a different line from the
Shipped `X` it removes) and `is_symbol_baseline` (the DRIVERS row's pattern), read by `landing_check.baseline_stops`, arm 0b:
it lists the baselines of the landing's own tree and of the tree `git merge-tree` makes of `origin/main` and the landing with
no driver configured, and stops (exit 3) naming file and symbol. A finding of the landing's own tree is not repeated for the
merge; a merge that conflicts has no tree to read (its markers are no symbols) and is the queue's conflict. The sweep of the
other `merge=union` files found none left in `.gitattributes`; the work notes' body union keeps a doubled line visible to a
reader and no build reads it. Self-test arms: five in `landing_check.py --self-test` (duplicate in a file, Shipped/Unshipped
overlap, `*REMOVED*` beside its Shipped symbol lands, the queue's merge duplicating, the same-place twin merging once) and two in
`merge_drivers.py --self-test`, declared in `MergeDriverDriftTests.TheSelfTests_DeclareEveryArm`. Design: DESIGN-orchestrator-loop.md
section 16.9 gained the paragraph, and DESIGN-test-build-ci.md's list of landing checks names the new arm.

Self-review: architecture (the rule lives beside the merge it guards, merge_drivers; the check only reads trees); full code
(the stop is exit 3 like every landing_check stop; a missing tree or a conflicting merge-tree reads as nothing to check, said in
the docstring); performance (one merge-tree, two ls-trees, one batched blob read per landing); duplication (no second reader
of the range, no second definition of which files are baselines). Rows closed: none (process tooling).
