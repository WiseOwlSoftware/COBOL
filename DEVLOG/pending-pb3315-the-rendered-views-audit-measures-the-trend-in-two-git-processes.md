## PB3315 and PB3647: the rendered-views audit measures the ledger's missed trend points in two git processes, and the .gitignore stops naming a deleted test

PB3315. The gate's pre-slot `audit RENDERED VIEWS` ran `rendered_views.py --check`, whose ledger view runs
`gen_ledger.py --check`. That check rebuilds the trend series, and `missed_points` measured every commit since the
series' last recorded point from its own tree: a `git show` of the 7.5 MB inventory twice, a `git ls-tree` and a
`git cat-file --batch` over all ~2,900 notes, per commit. Re-probed on this branch, machine idle, 81 commits behind:
334 git processes and 145.6 s (117.8 s in `cat-file`, 8.5 s in `git show`). Only a render of main's page appends to the
series, so the walk grows until main renders, and under ten concurrent gates each process start cost far more: the
20-28 minutes the note recorded, paid before the gate slot where the slot cap cannot bound it.

The fix is in the shape of the read, not a longer timeout. `scripts/git_batch.py` gained `tree_entries` (the files of
many `<rev>:<dir>` trees, one `cat-file --batch`) and `blobs` (the content of the distinct object ids, one more).
`gen_ledger.measure_commits` reads every commit's `kb/Work` and inventory trees in the first and the distinct blobs in
the second, so a note or inventory that two commits share is read and parsed once. `inventory_at`,
`inventory_counts_at`, `notes_at` and `register_counts_at` are deleted and every caller moved (`missed_points`,
`backfill_register`, `gap_at`, the self-test); the recorded-commit filter is `git_batch.resolve` and each point's label
rides the one `git log`. One `--check` now starts 12 processes and takes 9.7 s, the page it renders is byte-count
identical (209,687), and `rendered_views.py --check` takes 11 s. Equivalence was measured against the old functions on
60 real commits, HEAD and a bogus sha: no difference.

Guard: `gen_ledger.py --self-test` counts the processes of `missed_points` over 3 and over 15 planted commits and
requires the counts equal and at most 6 (a structural bound; the old shape would have started 15 and 75).
`git_batch.py --self-test` holds `tree_entries` and `blobs` to `ls-tree` and `cat-file blob`, and GitBatchDriftTests
names the new arms. DESIGN-test-build-ci.md (section 3.17.4 and the batched-readers paragraph) carries the new readers
and the cost.

PB3647. The `.gitignore` RENDERED VIEWS comment named `RenderedViewsDriftTests`, which PB2878 deleted. It now names
`rendered_views.py --check` (the gate's `RENDERED VIEWS` audit and CI's `audits` job), and the script's own docstring no
longer calls rule (a) a drift test. A sweep of docs, scripts, workflows and tests found no other mention of the deleted
test or of `rendered_views.py --tree` outside history paragraphs that record the deletion.

Self-review: silent failures (a tree or blob git cannot read raises GitError and fails `--check`, never an empty
answer; an unreadable inventory is None exactly as before), tests (the arms above), comments (the old PB3026 text in the
docstring corrected), no new type beyond the two-field `Committed` tuple. No inventory row is claimed or closed; both
notes carry `closes_rows_reason`.
