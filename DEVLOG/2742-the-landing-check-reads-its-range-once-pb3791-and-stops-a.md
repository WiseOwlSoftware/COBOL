## Entry 2742 — 2026-10-29 15:11 PDT — The landing check reads its range once (PB3791) and stops a public-API symbol listed twice (PB3694)

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
case that bounced PR #541 with RS0025 came from the queue's driverless merge of a head and a main that had advanced. The rest of the note's fix direction is now in: `merge_drivers.baseline_duplicates` (a symbol twice in one file, or one
plain line in two files of one directory, which is how the analyzer counts RS0025; `*REMOVED*X` is a different line from the
Shipped `X` it removes) and `is_symbol_baseline` (the DRIVERS row's pattern), read by `landing_check.baseline_stops`, arm 0b:
it lists the baselines of the landing's own tree and stops (exit 3) naming file and symbol. Branch review round 1 (R1-1)
overturned a first version that also read the driverless `git merge-tree` of `origin/main` and the landing: every caller
presents a head that contains the base, so that tree is the head's own and the half never ran, and the self-test arm pinned a
state no caller presents. It is removed, and the residual stated in place of the claim: the queue's merge differs from the head
only when main advances after the push, which no check before the push can read; CI's merge-group run is the net. The sweep of the
other `merge=union` files found none left in `.gitattributes`; the work notes' body union keeps a doubled line visible to a
reader and no build reads it. Self-test arms: three in `landing_check.py --self-test` (duplicate in a file, Shipped/Unshipped
overlap, `*REMOVED*` beside its Shipped symbol lands) and two in
`merge_drivers.py --self-test`, declared in `MergeDriverDriftTests.TheSelfTests_DeclareEveryArm`. Design: DESIGN-orchestrator-loop.md
section 16.9 gained the paragraph, and DESIGN-test-build-ci.md's list of landing checks names the new arm.

Self-review: architecture (the rule lives beside the merge it guards, merge_drivers; the check only reads a tree); full code
(the stop is exit 3 like every landing_check stop, which the lane reads as a wait: see the report's lead); performance (one
ls-tree and one batched blob read per landing); duplication (no second reader
of the range, no second definition of which files are baselines). Rows closed: none (process tooling).

