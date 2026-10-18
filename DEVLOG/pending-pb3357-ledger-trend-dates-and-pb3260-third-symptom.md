## Filed PB3357 (ledger trend dates mix UTC and local offsets) and a third PB3260 symptom

Two leads from the enqueue unit became register entries. PB3357 records that `gen_ledger.py` labels trend points with the committer's local calendar date, so UTC merge-queue commits and local PDT commits interleave and the page's dates step backward (at least 13 times in 983 points). PB3260 gains a third symptom: a wave 1113 report whose HEAD field held prose instead of a sha was judged uncovered by `prune_worktrees.lifecycle()` before its CLEAN review stamp was read. No code changed.
