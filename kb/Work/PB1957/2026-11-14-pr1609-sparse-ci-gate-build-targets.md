## 2026-11-14: PR #1609 bounced because `ci-gate`'s sparse checkout left out a file `test_population.py` reads (class (e), GitHub's runners)

Pull request #1609 (branch `worktree-wf_52dcfb9a-25b-1`, landed head e301acbed3; kb/Work PB5163's group, which carried
PB5230) failed merge-group run 43552030404 in job `ci-gate`, step "The shards' union is exactly the discovered
population". Every shard passed. Each `test_population.py check` call died with `FileNotFoundError:
.../scripts/build_targets.json` (`scripts/test_population.py` line 106, `GATED_ASSEMBLIES`, which PB5230's review R1-4
made read the targets file at import), so every assembly on both OSes read POPULATION MISMATCH. #1607 and #1610, queued
behind it, failed the same way and re-ran once GitHub ejected #1609.

Why no local run saw it: `ci-gate` checks out only its sparse list, and the local gate runs on a whole tree. The audit
that models the list, `scripts/sparse_checkout.py`, followed a Python script's imports but not the files it reads by a
literal path, which its own docstring named as a limit. So the check was GREEN on a job that could not run.

The fix (same branch): `scripts/build_targets.json` is in both of `ci-gate`'s lists, and `sparse_checkout.py` now
follows the files a Python script reads by a literal path (a `/` chain or string constant in the receiver of
`read_text`, `read_bytes` or `open`, or the argument of `open(`). The audit skips reads inside the script's own
`self_test` function and treats a read file as data, not followed further. With the workflow fix reverted, `--check` is RED
on exactly this file and the new real-tree arm of `--self-test` fails. A computed path (a variable, an f-string, a glob)
is still not seen.

What this adds to the parity record:
- **Class (e), a CI job's checkout that holds less than the local tree.** The merge group is the first run on a sparse
  tree. A script change that adds a data read ships green locally and red in CI unless the sparse audit models the
  read. That audit now models literal reads. Computed paths remain the limit.
