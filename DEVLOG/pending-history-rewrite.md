## The history rewrite: the per-copy licence stamp removed from every commit; old SHAs remapped (kb/Work PB4814)

At the owner's request the repository's whole history was rewritten with git filter-repo to remove the reseller's per-copy
licence stamp (purchaser's name, order number, download date) from every blob and commit message, and `main` and the tags
were force-pushed (08798ec72 -> 16f4391f5) with the merge-queue ruleset off for that one push. This landing moves the
current tree's about 8,400 cited SHAs in 3,181 files to the new commits (same length, so fixed-format sources keep their columns),
and adds the rewrite's commit map (`docs/history/commit-map-2026-11-05.txt`) with `scripts/history_sha.py` to translate an old
SHA. GitHub's purge of the old commits from pull-request refs is the owner's to request; PB4814 stays half until then.
