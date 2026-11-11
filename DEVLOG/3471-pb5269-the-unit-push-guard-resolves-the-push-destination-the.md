## Entry 3471 — 2026-11-11 03:01 PST — PB5269: the unit-push guard resolves the push destination the way git does (review leads of PB5234)

The branch review of PB5234 found that the in-unit push rule judged a push's repository by the first word without a dash.
`git push --repo=https://github.com/WiseOwlSoftware/claude-skills.git` therefore read as `origin` and passed, and a remote's
separate push URL or the configured default push remote (`remote.pushDefault`, `branch.<b>.pushRemote`) was never looked
at: the PB2044 harm (a unit publishing another repository) returned by three doors.

`_push_urls` now resolves the destination as git does: `--repo=`/`--repo X`, else the first positional word, else
`branch.<b>.pushRemote`, `remote.pushDefault`, `branch.<b>.remote`, `origin`; a remote name is read through
`git remote get-url --push --all` (pushurl over url); the call's own `-c` options reach git; every URL must be this
repository's origin. New self-test cases run against ScratchRepo repositories whose origin is this repository's and fail on
the old rule (9 of the new cases fail against it; the `--repo=<url>` cases without a refspec are needed because the old
rule blocked the with-refspec form only by reading `HEAD` as a remote name). Three cases that had been flipped to expect
"not blocked" (`QS + git push`, `xargs git push`, backquoted `git push`) are pairs again, one pointed at another repository.

Also: `Step.substitution`, read by nothing but its own self-test, is deleted, the rule numbers in `shell_location.py` and its
self-test are corrected, and DESIGN-test-build-ci.md §1.7 describes the depth instead. kb/Work PB5269.

Review round 1 corrected two things in the same change. The positional repository word now wins over `--repo` (git's own
order; the first fix had it reversed, so `--repo=origin <url>` passed). And a push redirected by `--git-dir`, a `GIT_DIR`-style
variable set in the line, or a `url.*.insteadOf` / `pushInsteadOf` (on `-c` or in the repository's configuration, for a literal
URL) is refused: the hook cannot prove where it goes.

